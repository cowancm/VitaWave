#!/usr/bin/env python3
"""
Main Script for TI 6843 ISK ODS Point Cloud Visualizer

This script serves as the entry point for the TI 6843 ISK ODS point cloud visualization system.
It initializes the visualizer and handles high-level application flow.

Usage:
    python main.py

Requirements:
    - Open3D
    - NumPy
    - PySerial
    - Keyboard
"""

import os
import sys
import time
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("radar_visualizer.log"),
        logging.StreamHandler()
    ]
)
log = logging.getLogger(__name__)

def check_dependencies():
    """Check if all required dependencies are installed."""
    try:
        import open3d
        import numpy
        import serial
        import keyboard
        return True
    except ImportError as e:
        log.error(f"Missing dependency: {e}")
        print(f"\nError: Missing dependency: {e}")
        print("Please install all required dependencies:")
        print("  pip install open3d numpy pyserial keyboard")
        return False

def check_configs_directory():
    """Check if the configs directory exists and contains .cfg files."""
    configs_dir = "configs"
    if not os.path.exists(configs_dir):
        os.makedirs(configs_dir)
        log.warning(f"Created directory '{configs_dir}'. Please add configuration files.")
        print(f"\nCreated directory '{configs_dir}'.")
        print("Please add your TI 6843 configuration files (.cfg) to this directory.")
        print("Then restart the application.")
        return False
    
    cfg_files = [f for f in os.listdir(configs_dir) if f.endswith('.cfg')]
    if not cfg_files:
        log.warning(f"No .cfg files found in {configs_dir}")
        print(f"\nNo configuration files found in '{configs_dir}'.")
        print("Please add your TI 6843 configuration files (.cfg) to this directory.")
        print("Then restart the application.")
        return False
    
    return True

def main():
    """Main function to run the TI 6843 Point Cloud Visualizer."""
    print("╔═══════════════════════════════════════════════════════════╗")
    print("║           TI 6843 ISK ODS Point Cloud Visualizer          ║")
    print("╚═══════════════════════════════════════════════════════════╝")
    
    # Check dependencies
    if not check_dependencies():
        return
    
    # Check configs directory
    if not check_configs_directory():
        return
    
    # Import visualizer here to avoid import errors if dependencies are missing
    from integrated_visualizer import PointCloudVisualizer
    
    try:
        print("\nInitializing visualizer...")
        visualizer = PointCloudVisualizer()
        
        print("\nPoint Cloud Visualizer Controls:")
        print("  'R' - Toggle between realtime and playback modes")
        print("  'S' - Save the last 50 frames to a .npy file (only in realtime mode)")
        print("       You will be prompted to enter a filename")
        print("  'L' - Load playback data when in playback mode")
        print("       You will be shown available files to choose from")
        print("  'SPACE' - Cycle to next frame in playback mode")
        print("  Left click and drag - Rotate view")
        print("  Right click and drag - Pan view")
        print("  Mouse wheel - Zoom in/out")
        print("  ESC - Close the visualizer")
        
        print("\nStarting the visualizer. Please follow the prompts to connect to the radar...")
        visualizer.run()
        
    except Exception as e:
        log.error(f"Error running visualizer: {e}", exc_info=True)
        print(f"\nError: {e}")
        print("See radar_visualizer.log for more details.")
    
    finally:
        print("\nVisualizer closed. Thank you for using TI 6843 ISK ODS Point Cloud Visualizer.")

if __name__ == "__main__":
    main()
