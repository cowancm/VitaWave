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
        self.vis.register_key_callback(32, self.space_callback) #32 for space
        
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
            
    def run(self):
        # Run the visualizer
        self.vis.run()
        self.vis.destroy_window()


# entry point, make a thread and run this guy and pass in the list of frames
def start(frames):
    vis = PointCloudVisualizer(frames)
    vis.run()


