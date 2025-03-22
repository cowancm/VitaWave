import open3d as o3d
import numpy as np
import copy

class PointCloudVisualizer:
    def __init__(self, point_clouds):
        self.point_clouds = point_clouds  # List of point clouds
        self.current_idx = 0
        self.vis = o3d.visualization.VisualizerWithKeyCallback()
        self.vis.create_window()
        
        # Register space bar callback
        self.vis.register_key_callback(32, self.space_callback)  # 32 is the ASCII code for space
        
        # Add initial point cloud
        self.pcd = o3d.geometry.PointCloud()
        self.update_point_cloud()
        self.vis.add_geometry(self.pcd)
        
        # Add a floor grid on the XY plane
        self.add_floor_grid()
        
        # Set some view control settings
        self.vis.get_render_option().point_size = 2.0
        self.vis.get_render_option().background_color = np.array([0.1, 0.1, 0.1])
        
        # Set camera position to face the point cloud
        self.setup_camera()
        
    def setup_camera(self):
        """Set up the camera to face the point cloud from the front."""
        # Get the bounding box of the current point cloud
        bbox = self.pcd.get_axis_aligned_bounding_box()
        center = bbox.get_center()
        
        # Calculate appropriate distance based on bounding box size
        bbox_extent = bbox.get_extent()
        max_extent = max(bbox_extent)
        distance = max_extent * 2.5  # Adjust this multiplier as needed
        
        # Set up camera parameters
        view_control = self.vis.get_view_control()
        
        # Position camera in front of the point cloud
        # Assuming "front" means looking towards the object along the -Y axis
        # With the person standing on the XY plane
        front_pos = np.array([center[0], center[1] + distance, center[2] + max_extent * 0.5])
        up_vector = np.array([0, 0, 1])  # Z is up
        
        # Set the camera parameters
        view_control.set_lookat(center)
        view_control.set_front(front_pos - center)
        view_control.set_up(up_vector)
        view_control.set_zoom(0.7)
        
    def add_floor_grid(self, size=10, grid_count=10, grid_color=[0.5, 0.5, 0.5]):
        """Add a grid on the XY plane to serve as a floor."""
        # Calculate grid step based on size and count
        grid_step = size * 2 / grid_count
        
        # Create grid lines along X axis
        for i in range(grid_count + 1):
            x_pos = -size + i * grid_step
            line_points = np.array([
                [-size, x_pos, 0],
                [size, x_pos, 0]
            ])
            line = o3d.geometry.LineSet()
            line.points = o3d.utility.Vector3dVector(line_points)
            line.lines = o3d.utility.Vector2iVector(np.array([[0, 1]]))
            line.colors = o3d.utility.Vector3dVector(np.array([grid_color]))
            self.vis.add_geometry(line)
            
        # Create grid lines along Y axis
        for i in range(grid_count + 1):
            y_pos = -size + i * grid_step
            line_points = np.array([
                [y_pos, -size, 0],
                [y_pos, size, 0]
            ])
            line = o3d.geometry.LineSet()
            line.points = o3d.utility.Vector3dVector(line_points)
            line.lines = o3d.utility.Vector2iVector(np.array([[0, 1]]))
            line.colors = o3d.utility.Vector3dVector(np.array([grid_color]))
            self.vis.add_geometry(line)
            
    def space_callback(self, vis):
        # Cycle to the next point cloud
        self.current_idx = (self.current_idx + 1) % len(self.point_clouds)
        self.update_point_cloud()
        self.vis.update_geometry(self.pcd)
        self.vis.poll_events()
        self.vis.update_renderer()
        return False
    
    def update_point_cloud(self):
        # Update the point cloud with the current frame
        self.pcd.points = self.point_clouds[self.current_idx].points
        
        # If the point clouds have colors, update those too
        if hasattr(self.point_clouds[self.current_idx], 'colors'):
            self.pcd.colors = self.point_clouds[self.current_idx].colors
            
        print(f"Showing frame {self.current_idx + 1}/{len(self.point_clouds)}")
        
    def run(self):
        # Run the visualizer
        self.vis.run()
        self.vis.destroy_window()


# Example usage:
if __name__ == "__main__":
    # Create some sample point clouds (replace with your actual data)
    point_clouds = []
    for i in range(5):
        # Create a more person-like point cloud for demonstration
        pcd = o3d.geometry.PointCloud()
        
        # Create a simple person-like shape (vertical cylinder with a sphere on top)
        num_points = 3000
        
        # Body points (cylinder-like)
        theta = np.random.uniform(0, 2*np.pi, num_points * 2//3)
        h = np.random.uniform(0, 1.7, num_points * 2//3)  # Height up to 1.7 units
        r = 0.3 * np.random.uniform(0.8, 1.0, num_points * 2//3)  # Radius around 0.3 units
        
        body_x = r * np.cos(theta)
        body_y = r * np.sin(theta)
        body_z = h
        
        # Head points (sphere-like)
        phi = np.random.uniform(0, 2*np.pi, num_points//3)
        theta = np.random.uniform(0, np.pi, num_points//3)
        r = 0.2 * np.random.uniform(0.8, 1.0, num_points//3)  # Head radius
        
        head_x = r * np.sin(theta) * np.cos(phi)
        head_y = r * np.sin(theta) * np.sin(phi)
        head_z = 1.7 + r * np.cos(theta)  # Place head on top of body
        
        # Combine points
        x = np.concatenate([body_x, head_x])
        y = np.concatenate([body_y, head_y])
        z = np.concatenate([body_z, head_z])
        
        # Add slight variations for each frame
        points = np.vstack([x, y, z]).T + np.array([0, 0, 0.05 * i])
        
        pcd.points = o3d.utility.Vector3dVector(points)
        
        # Add colors (e.g., blue for body, pink for head)
        colors = np.zeros((len(points), 3))
        colors[:num_points*2//3] = [0.1, 0.6, 0.9]  # Blue for body
        colors[num_points*2//3:] = [0.9, 0.5, 0.7]  # Pink for head
        
        # Add some random variation to colors
        colors += np.random.uniform(-0.1, 0.1, size=colors.shape)
        colors = np.clip(colors, 0, 1)  # Ensure colors are in valid range
        
        pcd.colors = o3d.utility.Vector3dVector(colors)
        
        point_clouds.append(pcd)
    
    # Create and run the visualizer
    vis = PointCloudVisualizer(point_clouds)
    vis.run()