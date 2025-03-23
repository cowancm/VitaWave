import struct
import serial
import time
import logging
import os
import threading
import queue
import numpy as np
from datetime import datetime

# Constants for TLV parsing
UART_MAGIC_WORD = bytearray(b'\x02\x01\x04\x03\x06\x05\x08\x07')
MMWDEMO_OUTPUT_MSG_DETECTED_POINTS = 1
MMWDEMO_OUTPUT_MSG_DETECTED_POINTS_SIDE_INFO = 7
MMWDEMO_OUTPUT_EXT_MSG_DETECTED_POINTS = 301
MMWDEMO_OUTPUT_MSG_COMPRESSED_POINTS = 1020
MMWDEMO_OUTPUT_MSG_TRACKERPROC_3D_TARGET_LIST = 1010
MMWDEMO_OUTPUT_MSG_TRACKERPROC_TARGET_INDEX = 1011
MMWDEMO_OUTPUT_MSG_TRACKERPROC_TARGET_HEIGHT = 1012
MMWDEMO_OUTPUT_MSG_PRESCENCE_INDICATION = 1021

# Configure logging
logging.basicConfig(level=logging.INFO)
log = logging.getLogger(__name__)

class TI6843UARTParser:
    """
    Class to handle UART communication with TI 6843 ISK ODS and parse TLV data for visualization.
    """
    def __init__(self, data_queue):
        """
        Initialize the UART parser.
        
        Args:
            data_queue: Queue to put processed point cloud data into for visualization
        """
        # Communication ports
        self.cli_com = None
        self.data_com = None
        
        # Data storage
        self.data_queue = data_queue
        self.running = False
        self.parser_thread = None
        
        # Configuration
        self.config_file = None
        self.save_binary = False
        self.bin_data_dir = "radar_data"
        self.bin_data_filename = datetime.now().strftime("%Y%m%d_%H%M%S")
        
        # Frame data
        self.frame_count = 0
        self.frames_per_file = 100
        
    def connect_com_ports(self, cli_port, data_port, cli_baud=115200, data_baud=921600):
        """
        Connect to the COM ports for the TI 6843 device.
        
        Args:
            cli_port: Command port (e.g., "COM3")
            data_port: Data port (e.g., "COM4")
            cli_baud: Command port baud rate
            data_baud: Data port baud rate
        """
        try:
            self.cli_com = serial.Serial(
                cli_port, 
                cli_baud,
                parity=serial.PARITY_NONE,
                stopbits=serial.STOPBITS_ONE,
                timeout=0.6
            )
            
            self.data_com = serial.Serial(
                data_port, 
                data_baud,
                parity=serial.PARITY_NONE,
                stopbits=serial.STOPBITS_ONE,
                timeout=0.6
            )
            
            self.data_com.reset_output_buffer()
            log.info(f"Connected to {cli_port} (CLI) and {data_port} (Data)")
            return True
        
        except serial.SerialException as e:
            log.error(f"Failed to connect to COM ports: {e}")
            return False
    
    def send_config(self, config_file):
        """
        Send configuration to the device.
        
        Args:
            config_file: Path to the configuration file
        """
        try:
            # Read the configuration file
            with open(config_file, 'r') as f:
                cfg_lines = f.readlines()
            
            # Store the config filename
            self.config_file = os.path.basename(config_file)
            
            # Process and send the configuration
            # Remove empty lines and comments from the config
            cfg_lines = [line for line in cfg_lines if line.strip() and not line.strip().startswith('%')]
            
            # Ensure each line ends with a newline
            cfg_lines = [line.strip() + '\n' for line in cfg_lines]
            
            log.info(f"Sending configuration from {config_file}...")
            
            # Send each line with a delay
            for line in cfg_lines:
                time.sleep(0.03)  # Line delay
                self.cli_com.write(line.encode())
                
                # Read acknowledgement
                ack = self.cli_com.readline()
                log.debug(f"ACK: {ack}")
                
                # Handle baud rate changes if needed
                split_line = line.split()
                if len(split_line) > 1 and split_line[0] == "baudRate":
                    try:
                        new_baud = int(split_line[1])
                        log.info(f"Changing CLI baud rate to {new_baud}")
                        self.cli_com.baudrate = new_baud
                    except Exception as e:
                        log.error(f"Error changing baud rate: {e}")
            
            time.sleep(0.1)  # Allow time for the device to process the configuration
            self.cli_com.reset_input_buffer()
            
            log.info("Configuration sent successfully")
            return True
            
        except Exception as e:
            log.error(f"Error sending configuration: {e}")
            return False
    
    def start_parsing(self):
        """
        Start the parser thread to read and parse data from the radar.
        """
        if self.parser_thread is not None and self.parser_thread.is_alive():
            log.warning("Parser is already running")
            return False
        
        self.running = True
        self.parser_thread = threading.Thread(target=self._parser_thread_function)
        self.parser_thread.daemon = True
        self.parser_thread.start()
        
        log.info("Parser thread started")
        return True
    
    def stop_parsing(self):
        """
        Stop the parser thread.
        """
        self.running = False
        if self.parser_thread is not None:
            self.parser_thread.join(timeout=1.0)
            self.parser_thread = None
        
        log.info("Parser thread stopped")
    
    def _parser_thread_function(self):
        """
        Main thread function for reading and parsing radar data.
        """
        # Create directory for binary data if saving is enabled
        if self.save_binary:
            if not os.path.exists(self.bin_data_dir):
                os.makedirs(self.bin_data_dir)
        
        # Buffer for binary data
        binary_buffer = bytearray()
        
        while self.running:
            try:
                # Read and parse one frame
                frame_data = self._read_frame()
                if frame_data:
                    # Parse the frame to extract point cloud
                    point_cloud = self._parse_frame(frame_data)
                    
                    # If we have points, put them in the queue for visualization
                    if point_cloud is not None and len(point_cloud) > 0:
                        self.data_queue.put(point_cloud)
                    
                    # Save binary data if enabled
                    if self.save_binary:
                        binary_buffer.extend(frame_data)
                        self.frame_count += 1
                        
                        # Save accumulated frames periodically
                        if self.frame_count % self.frames_per_file == 0:
                            file_path = os.path.join(
                                self.bin_data_dir,
                                f"{self.bin_data_filename}_{self.frame_count // self.frames_per_file}.bin"
                            )
                            with open(file_path, 'wb') as f:
                                f.write(binary_buffer)
                            
                            log.info(f"Saved {self.frames_per_file} frames to {file_path}")
                            binary_buffer = bytearray()
            
            except Exception as e:
                log.error(f"Error in parser thread: {e}")
                time.sleep(0.1)  # Brief pause before retrying
    
    def _read_frame(self):
        """
        Read a complete frame from the data port.
        
        Returns:
            bytearray containing the frame data or None if error
        """
        try:
            # Find magic word to synchronize
            index = 0
            frame_data = bytearray()
            
            while True:
                byte = self.data_com.read(1)
                
                if len(byte) < 1:
                    log.warning("Timeout while reading from data port")
                    return None
                
                if byte[0] == UART_MAGIC_WORD[index]:
                    frame_data.append(byte[0])
                    index += 1
                    if index == 8:  # Found complete magic word
                        break
                else:
                    # Reset if we didn't find magic word
                    if index > 0:
                        # Check if the current byte could be the start of a new magic word
                        if byte[0] == UART_MAGIC_WORD[0]:
                            index = 1
                            frame_data = bytearray(byte)
                        else:
                            index = 0
                            frame_data = bytearray()
            
            # Read version and packet length
            version_bytes = self.data_com.read(4)
            frame_data.extend(version_bytes)
            
            length_bytes = self.data_com.read(4)
            frame_data.extend(length_bytes)
            
            # Convert packet length to int
            packet_length = int.from_bytes(length_bytes, byteorder='little')
            
            # Calculate remaining bytes to read (total - magic word - version - length)
            remaining_bytes = packet_length - 16
            
            # Read the rest of the frame
            remaining_data = self.data_com.read(remaining_bytes)
            frame_data.extend(remaining_data)
            
            return frame_data
            
        except Exception as e:
            log.error(f"Error reading frame: {e}")
            return None
    
    def _parse_frame(self, frame_data):
        """
        Parse a frame of data to extract the point cloud with x,y,z,v format.
        
        Args:
            frame_data: bytearray containing the frame data
            
        Returns:
            numpy array with shape (n, 4) where each row is [x, y, z, v]
        """
        try:
            # Constants for parsing
            header_struct = 'Q8I'
            header_length = struct.calcsize(header_struct)
            tlv_header_length = 8
            
            # Parse frame header
            (magic, version, total_packet_len, platform, frame_num, 
             time_cpu_cycles, num_detected_obj, num_tlvs, sub_frame_num) = struct.unpack(
                header_struct, frame_data[:header_length]
            )
            
            # Skip to the TLV data
            tlv_data = frame_data[header_length:]
            
            # Initialize point cloud data - use twice the size to be safe
            max_points = num_detected_obj * 2 if num_detected_obj > 0 else 100
            point_cloud = np.zeros((max_points, 4), dtype=np.float32)
            points_added = 0
            
            # Process each TLV
            for i in range(num_tlvs):
                # Parse TLV header
                if len(tlv_data) < tlv_header_length:
                    log.warning("Incomplete TLV header")
                    break
                
                tlv_type, tlv_length = struct.unpack('2I', tlv_data[:tlv_header_length])
                tlv_payload = tlv_data[tlv_header_length:tlv_header_length + tlv_length]
                
                # Process standard point cloud TLV
                if tlv_type == MMWDEMO_OUTPUT_MSG_DETECTED_POINTS:
                    log.debug(f"Found point cloud TLV (Type 1)")
                    points_added = self._parse_point_cloud_tlv(tlv_payload, tlv_length, point_cloud, points_added)
                
                # Process extended point cloud TLV
                elif tlv_type == MMWDEMO_OUTPUT_EXT_MSG_DETECTED_POINTS:
                    log.debug(f"Found extended point cloud TLV (Type 301)")
                    points_added = self._parse_point_cloud_ext_tlv(tlv_payload, tlv_length, point_cloud, points_added)
                
                # Process compressed point cloud TLV (for 3D-person tracking)
                elif tlv_type == MMWDEMO_OUTPUT_MSG_COMPRESSED_POINTS:
                    log.debug(f"Found compressed point cloud TLV (Type 1020)")
                    points_added = self._parse_compressed_point_cloud_tlv(tlv_payload, tlv_length, point_cloud, points_added)
                
                # Move to next TLV
                tlv_data = tlv_data[tlv_header_length + tlv_length:]
            
            # Return only the filled part of the point cloud
            return point_cloud[:points_added] if points_added > 0 else None
            
        except Exception as e:
            log.error(f"Error parsing frame: {e}")
            return None
    
    def _parse_point_cloud_tlv(self, tlv_data, tlv_length, point_cloud, points_added):
        """
        Parse standard point cloud TLV.
        
        Args:
            tlv_data: TLV payload
            tlv_length: Length of the TLV payload
            point_cloud: Array to fill with point cloud data
            points_added: Current number of points added
            
        Returns:
            Updated number of points added
        """
        point_struct = '4f'  # x, y, z, doppler
        point_size = struct.calcsize(point_struct)
        num_points = tlv_length // point_size
        
        for i in range(num_points):
            if points_added >= len(point_cloud):
                # Expand point cloud array if needed
                point_cloud = np.vstack([point_cloud, np.zeros((num_points, 4), dtype=np.float32)])
            
            offset = i * point_size
            if offset + point_size <= len(tlv_data):
                x, y, z, doppler = struct.unpack(point_struct, tlv_data[offset:offset + point_size])
                point_cloud[points_added] = [x, y, z, doppler]
                points_added += 1
            else:
                log.warning("Incomplete point data in TLV")
                break
        
        return points_added
    
    def _parse_point_cloud_ext_tlv(self, tlv_data, tlv_length, point_cloud, points_added):
        """
        Parse extended point cloud TLV (type 301).
        
        Args:
            tlv_data: TLV payload
            tlv_length: Length of the TLV payload
            point_cloud: Array to fill with point cloud data
            points_added: Current number of points added
            
        Returns:
            Updated number of points added
        """
        # Extended TLV has decompression factors followed by compressed points
        p_unit_struct = '4f2h'  # Units for decompression
        p_unit_size = struct.calcsize(p_unit_struct)
        
        # Point structure is different for extended TLV
        point_struct = '4h2B'  # x, y, z, doppler, snr, noise (compressed)
        point_size = struct.calcsize(point_struct)
        
        # Get decompression factors
        if len(tlv_data) < p_unit_size:
            log.warning("Incomplete decompression data in extended TLV")
            return points_added
        
        p_unit = struct.unpack(p_unit_struct, tlv_data[:p_unit_size])
        
        # Calculate number of points
        num_points = (tlv_length - p_unit_size) // point_size
        
        # Process each point
        for i in range(num_points):
            if points_added >= len(point_cloud):
                # Expand point cloud array if needed
                point_cloud = np.vstack([point_cloud, np.zeros((num_points, 4), dtype=np.float32)])
            
            offset = p_unit_size + (i * point_size)
            if offset + point_size <= len(tlv_data):
                x, y, z, doppler, _, _ = struct.unpack(point_struct, tlv_data[offset:offset + point_size])
                
                # Decompress values using the units
                x_val = x * p_unit[0]
                y_val = y * p_unit[0]
                z_val = z * p_unit[0]
                doppler_val = doppler * p_unit[1]
                
                point_cloud[points_added] = [x_val, y_val, z_val, doppler_val]
                points_added += 1
            else:
                log.warning("Incomplete point data in extended TLV")
                break
        
        return points_added
    
    def _parse_compressed_point_cloud_tlv(self, tlv_data, tlv_length, point_cloud, points_added):
        """
        Parse compressed point cloud TLV (type 1020) for 3D-person tracking.
        
        Args:
            tlv_data: TLV payload
            tlv_length: Length of the TLV payload
            point_cloud: Array to fill with point cloud data
            points_added: Current number of points added
            
        Returns:
            Updated number of points added
        """
        # Format according to the provided documentation:
        # 5 float units followed by N points with compressed format
        
        # Parse decompression units
        p_unit_struct = '5f'  # elevationUnit, azimuthUnit, dopplerUnit, rangeUnit, snrUnit
        p_unit_size = struct.calcsize(p_unit_struct)
        
        # Point structure according to documentation
        # Each point is: elevation(int8), azimuth(int8), doppler(int16), range(uint16), snr(uint16)
        point_struct = 'bbhHH'  # Correct format: 1+1+2+2+2 = 8 bytes
        point_size = struct.calcsize(point_struct)
        
        if len(tlv_data) < p_unit_size:
            log.warning("Compressed point cloud TLV too short for decompression units")
            return points_added
            
        # Extract decompression units
        elevation_unit, azimuth_unit, doppler_unit, range_unit, snr_unit = struct.unpack(
            p_unit_struct, tlv_data[:p_unit_size])
        
        # Calculate number of points
        num_points = (tlv_length - p_unit_size) // point_size
        
        log.debug(f"Parsing compressed point cloud with {num_points} points")
        
        # Ensure we have enough space in our point cloud array
        if points_added + num_points > len(point_cloud):
            # Expand the array
            new_size = points_added + num_points
            temp = np.zeros((new_size, 4), dtype=np.float32)
            temp[:points_added] = point_cloud[:points_added]
            point_cloud = temp
            
        # Process each point
        for i in range(num_points):
            offset = p_unit_size + (i * point_size)
            if offset + point_size <= len(tlv_data):
                try:
                    # Extract raw compressed values (all 5 values)
                    elevation, azimuth, doppler, range_val, snr = struct.unpack(
                        point_struct, tlv_data[offset:offset + point_size])
                    
                    # Handle sign extension for 8-bit values if needed
                    if elevation >= 128:
                        elevation -= 256
                    if azimuth >= 128:
                        azimuth -= 256
                    
                    # Apply decompression units
                    elevation_rad = elevation * elevation_unit
                    azimuth_rad = azimuth * azimuth_unit
                    doppler_mps = doppler * doppler_unit
                    range_m = range_val * range_unit
                    
                    # Convert spherical to cartesian coordinates
                    # x = r * sin(azimuth) * cos(elevation)
                    # y = r * cos(azimuth) * cos(elevation)
                    # z = r * sin(elevation)
                    x = range_m * np.sin(azimuth_rad) * np.cos(elevation_rad)
                    y = range_m * np.cos(azimuth_rad) * np.cos(elevation_rad)
                    z = range_m * np.sin(elevation_rad)
                    
                    # Store point in x,y,z,v format
                    point_cloud[points_added] = [x, y, z, doppler_mps]
                    points_added += 1
                    
                except Exception as e:
                    log.warning(f"Error parsing compressed point: {e}")
            else:
                log.warning("Truncated point data in compressed point cloud TLV")
                break
                
        return points_added
    
    def set_save_binary(self, enable, directory=None):
        """
        Enable or disable saving of binary data.
        
        Args:
            enable: True to enable saving, False to disable
            directory: Directory to save binary data (default: "radar_data")
        """
        self.save_binary = enable
        if directory:
            self.bin_data_dir = directory
        
        # Reset the filename to current timestamp
        self.bin_data_filename = datetime.now().strftime("%Y%m%d_%H%M%S")
        
        log.info(f"Binary data saving {'enabled' if enable else 'disabled'}")
    
    def close(self):
        """
        Close the connections and clean up resources.
        """
        self.stop_parsing()
        
        if self.cli_com and self.cli_com.is_open:
            self.cli_com.close()
        
        if self.data_com and self.data_com.is_open:
            self.data_com.close()
        
        log.info("UART parser closed")
