import open3d as o3d
import numpy as np
import time
import os
import threading
import queue
import keyboard
from datetime import datetime
import sys
import serial.tools.list_ports
import logging
from uart_parser import TI6843UARTParser

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
log = logging.getLogger(__name__)

class PointCloudVisualizer:
    def __init__(self):
        # Set up save directory
        self.setup_save_directory()
        
        # Initialize the visualizer
        self.vis = o3d.visualization.VisualizerWithKeyCallback()
        self.vis.create_window(window_name="TI 6843 Point Cloud Visualizer", width=1280, height=720)
        
        # Point cloud object
        self.pcd = o3d.geometry.PointCloud()
        
        # Add empty point cloud to visualizer
        self.vis.add_geometry(self.pcd)
        
        # Set up coordinate frame
        coordinate_frame = o3d.geometry.TriangleMesh.create_coordinate_frame(size=0.5)
        self.vis.add_geometry(coordinate_frame)
        
        # Set camera viewpoint
        self.setup_camera()
        
        # Mode variables
        self.mode = "realtime"  # 'realtime' or 'playback'
        self.playback_index = 0
        self.playback_frames = []
        self.should_update = False  # Flag for updating point cloud in playback mode
        
        # Frame buffer for recording
        self.frame_buffer = []
        self.frame_buffer_size = 50  # Store last 50 frames
        
        # Queue for real-time data
        self.data_queue = queue.Queue()
        
        # State for user input
        self.waiting_for_filename = False
        self.waiting_for_loadfile = False
        self.user_input = ""
        self.user_input_purpose = ""  # 'save' or 'load'
        
        # TI 6843 UART parser
        self.uart_parser = None
        
        # Register callbacks
        self.register_callbacks()
        
        # Flag to indicate if the program is running
        self.running = True
    
    def setup_save_directory(self):
        """Set up the directory for saving playback files based on user input."""
        # Create base playback directory if it doesn't exist
        base_dir = "playback"
        if not os.path.exists(base_dir):
            os.makedirs(base_dir)
            print(f"Created base directory: {base_dir}")
        
        # Get subdirectories
        subdirs = [d for d in os.listdir(base_dir) 
                  if os.path.isdir(os.path.join(base_dir, d))]
        
        # Add default directory if no subdirectories exist
        if not subdirs:
            default_dir = os.path.join(base_dir, "default")
            os.makedirs(default_dir)
            subdirs = ["default"]
            print(f"Created default subdirectory: {default_dir}")
        
        # Display options to user
        print("\nAvailable directories for saving playback files:")
        for i, subdir in enumerate(subdirs, 1):
            print(f"  {i}. {subdir}")
        print(f"  {len(subdirs) + 1}. Create new directory")
        
        # Get user choice
        while True:
            try:
                choice = input("\nSelect directory number: ")
                choice = int(choice)
                
                if 1 <= choice <= len(subdirs):
                    selected_dir = subdirs[choice - 1]
                    self.save_dir = os.path.join(base_dir, selected_dir)
                    break
                elif choice == len(subdirs) + 1:
                    new_dir_name = input("Enter new directory name: ")
                    self.save_dir = os.path.join(base_dir, new_dir_name)
                    if not os.path.exists(self.save_dir):
                        os.makedirs(self.save_dir)
                    break
                else:
                    print("Invalid choice. Please try again.")
            except ValueError:
                print("Please enter a number.")
        
        print(f"Using directory: {self.save_dir}")
    
    def setup_camera(self):
        # Set a good camera viewpoint
        ctr = self.vis.get_view_control()
        ctr.set_front([0, 0, -1])
        ctr.set_lookat([0, 0, 0])
        ctr.set_up([0, -1, 0])
        ctr.set_zoom(0.8)
    
    def register_callbacks(self):
        # Register keyboard callbacks
        self.vis.register_key_callback(32, self.spacebar_callback)  # Spacebar (32) for next frame in playback
        self.vis.register_key_callback(ord('R'), self.toggle_mode_callback)  # 'R' to toggle mode
        self.vis.register_key_callback(ord('L'), self.load_playback_data_callback)  # 'L' to load playback data
        self.vis.register_key_callback(ord('S'), self.save_buffer_callback)  # 'S' to save frame buffer
    
    def spacebar_callback(self, vis):
        if self.mode == "playback" and len(self.playback_frames) > 0:
            self.should_update = True
        return False
    
    def toggle_mode_callback(self, vis):
        if self.mode == "realtime":
            self.mode = "playback"
            print("Switched to PLAYBACK mode. Press SPACE to cycle through frames, 'L' to load data.")
        else:
            self.mode = "realtime"
            print("Switched to REALTIME mode.")
        return False
    
    def save_buffer_callback(self, vis):
        """Start the process to save the current frame buffer when 'S' is pressed."""
        if self.mode == "realtime" and len(self.frame_buffer) > 0:
            if not self.waiting_for_filename:
                print("\nSaving frames...")
                print(f"Enter filename to save {len(self.frame_buffer)} frames (without .npy extension):")
                self.waiting_for_filename = True
                self.user_input = ""
                self.user_input_purpose = "save"
        else:
            if self.mode != "realtime":
                print("Frame saving is only available in realtime mode.")
            else:
                print("No frames in buffer to save.")
        return False
    
    def save_frames_to_file(self, filename):
        """Save the frame buffer to a file with the given name."""
        try:
            if not filename.endswith('.npy'):
                filename += '.npy'
                
            file_path = os.path.join(self.save_dir, filename)
            
            # Convert list to numpy array for saving
            frames_array = np.array(self.frame_buffer, dtype=object)
            
            # Save to .npy file
            np.save(file_path, frames_array)
            
            print(f"Saved {len(self.frame_buffer)} frames to {file_path}")
        except Exception as e:
            print(f"Error saving frame buffer: {e}")
        
        self.waiting_for_filename = False
    
    def load_playback_data_callback(self, vis):
        """Start the process to load playback data when 'L' is pressed."""
        if self.mode == "playback":
            # List available files
            available_files = [f for f in os.listdir(self.save_dir) if f.endswith('.npy')]
            
            if not available_files:
                print(f"No .npy files found in {self.save_dir}")
                return False
            
            print("\nAvailable playback files:")
            for i, file in enumerate(available_files, 1):
                print(f"  {i}. {file}")
            
            print("\nEnter the number or name of the file to load:")
            self.waiting_for_loadfile = True
            self.user_input = ""
            self.user_input_purpose = "load"
        else:
            print("Please switch to playback mode first by pressing 'R'")
        return False
    
    def load_playback_data(self, file_identifier):
        """Load point cloud frames from a single .npy file."""
        try:
            # Figure out if user entered a number or filename
            available_files = [f for f in os.listdir(self.save_dir) if f.endswith('.npy')]
            
            filename = None
            try:
                # Check if user entered a number
                index = int(file_identifier) - 1
                if 0 <= index < len(available_files):
                    filename = available_files[index]
            except ValueError:
                # User entered a filename
                if file_identifier.endswith('.npy'):
                    filename = file_identifier
                else:
                    filename = file_identifier + '.npy'
                    
                if filename not in available_files:
                    print(f"File not found: {filename}")
                    return
            
            if filename is None:
                print("Invalid file selection")
                return
            
            # Full path to the file
            file_path = os.path.join(self.save_dir, filename)
            
            # Load the frames
            self.playback_frames = np.load(file_path, allow_pickle=True)
            
            self.playback_index = 0
            print(f"Loaded {len(self.playback_frames)} frames from {filename}")
        except Exception as e:
            print(f"Error loading playback data: {e}")
            self.playback_frames = []
        
        self.waiting_for_loadfile = False
    
    def update_point_cloud(self, points):
        """Update the point cloud with new points data."""
        if len(points) == 0:
            return
        
        # Convert to numpy array if it's not already
        if not isinstance(points, np.ndarray):
            points = np.array(points, dtype=np.float64)
        
        # Handle x,y,z,v format by taking only the first 3 columns (xyz)
        if points.shape[1] >= 4:  # If we have at least 4 columns (x,y,z,v)
            points_xyz = points[:, :3]  # Take only x,y,z and ignore v
        else:
            points_xyz = points
        
        # Update the point cloud
        self.pcd.points = o3d.utility.Vector3dVector(points_xyz)
        
        # Generate colors for points (blue to red gradient based on height)
        if len(points_xyz) > 0:
            colors = np.zeros((len(points_xyz), 3))
            normalized_heights = (points_xyz[:, 2] - np.min(points_xyz[:, 2])) / (np.max(points_xyz[:, 2]) - np.min(points_xyz[:, 2]) + 1e-6)
            colors[:, 0] = normalized_heights  # R channel increases with height
            colors[:, 2] = 1 - normalized_heights  # B channel decreases with height
            self.pcd.colors = o3d.utility.Vector3dVector(colors)
        
        # Update the geometry
        self.vis.update_geometry(self.pcd)
    
    def handle_key_input(self, key):
        """Handle keyboard input for filename or file selection."""
        if not (self.waiting_for_filename or self.waiting_for_loadfile):
            return
            
        # Check for special keys
        if key == "backspace" and len(self.user_input) > 0:
            # Handle backspace
            self.user_input = self.user_input[:-1]
            # Clear current line and reprint
            print("\r" + " " * 50, end="\r")
            if self.user_input_purpose == "save":
                print(f"Enter filename: {self.user_input}", end="")
            else:
                print(f"Enter file number or name: {self.user_input}", end="")
        elif key == "enter":
            # Handle enter
            print()  # Move to next line
            if self.user_input:
                if self.user_input_purpose == "save":
                    self.save_frames_to_file(self.user_input)
                else:  # load
                    self.load_playback_data(self.user_input)
            else:
                print("Operation cancelled")
                self.waiting_for_filename = False
                self.waiting_for_loadfile = False
        elif len(key) == 1:  # Regular character
            # Add character to input
            self.user_input += key
            # Update display
            if self.user_input_purpose == "save":
                print(f"\rEnter filename: {self.user_input}", end="")
            else:
                print(f"\rEnter file number or name: {self.user_input}", end="")
    
    def setup_radar_connection(self):
        """Set up connection to the TI 6843 radar."""
        # List available COM ports
        ports = list(serial.tools.list_ports.comports())
        if not ports:
            print("No COM ports found. Please connect the TI 6843 and try again.")
            return False
        
        print("\nAvailable COM ports:")
        for i, port in enumerate(ports, 1):
            print(f"  {i}. {port.device} - {port.description}")
        
        # Get CLI port
        while True:
            try:
                cli_choice = input("\nSelect CLI port number: ")
                cli_index = int(cli_choice) - 1
                if 0 <= cli_index < len(ports):
                    cli_port = ports[cli_index].device
                    break
                print("Invalid choice. Please try again.")
            except ValueError:
                print("Please enter a number.")
        
        # Get data port
        while True:
            try:
                data_choice = input("Select Data port number: ")
                data_index = int(data_choice) - 1
                if 0 <= data_index < len(ports) and data_index != cli_index:
                    data_port = ports[data_index].device
                    break
                print("Invalid choice or same as CLI port. Please try again.")
            except ValueError:
                print("Please enter a number.")
        
        # List config files
        config_dir = "configs"
        if not os.path.exists(config_dir):
            os.makedirs(config_dir)
            print(f"\nCreated config directory: {config_dir}")
            print("Please place your configuration files in this directory and restart.")
            return False
        
        config_files = [f for f in os.listdir(config_dir) if f.endswith('.cfg')]
        if not config_files:
            print(f"\nNo configuration files found in {config_dir}.")
            print("Please add .cfg files and restart.")
            return False
        
        print("\nAvailable configuration files:")
        for i, cfg_file in enumerate(config_files, 1):
            print(f"  {i}. {cfg_file}")
        
        # Get config file
        while True:
            try:
                cfg_choice = input("\nSelect configuration file: ")
                try:
                    cfg_index = int(cfg_choice) - 1
                    if 0 <= cfg_index < len(config_files):
                        config_file = config_files[cfg_index]
                        break
                except ValueError:
                    # User might have entered the filename directly
                    if cfg_choice in config_files:
                        config_file = cfg_choice
                        break
                    elif cfg_choice + '.cfg' in config_files:
                        config_file = cfg_choice + '.cfg'
                        break
                print("Invalid choice. Please try again.")
            except ValueError:
                print("Please enter a valid selection.")
        
        config_path = os.path.join(config_dir, config_file)
        
        # Initialize UART parser
        self.uart_parser = TI6843UARTParser(self.data_queue)
        
        # Connect to ports
        if not self.uart_parser.connect_com_ports(cli_port, data_port):
            print("Failed to connect to COM ports.")
            return False
        
        # Send config
        if not self.uart_parser.send_config(config_path):
            print("Failed to send configuration.")
            return False
        
        # Start parsing
        if not self.uart_parser.start_parsing():
            print("Failed to start parsing.")
            return False
        
        print(f"\nSuccessfully connected to TI 6843 and started parsing with config: {config_file}")
        return True
        