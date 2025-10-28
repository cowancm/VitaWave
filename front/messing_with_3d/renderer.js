// Three.js scene setup
let scene, camera, renderer, points, geometry;
let isStreaming = false;
let streamInterval;
let frameCount = 0;
let lastTime = Date.now();

// Maximum number of points to display (adjust based on performance needs)
const MAX_POINTS = 50000;
let currentPointCount = 0;

// Initialize the 3D scene
function init() {
    const container = document.getElementById('canvas-container');
    
    // Create scene
    scene = new THREE.Scene();
    scene.background = new THREE.Color(0x000000);
    
    // Create camera
    camera = new THREE.PerspectiveCamera(
        75,
        window.innerWidth / window.innerHeight,
        0.1,
        1000
    );
    camera.position.z = 50;
    
    // Create renderer
    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    container.appendChild(renderer.domElement);
    
    // Create point cloud geometry
    geometry = new THREE.BufferGeometry();
    
    // Initialize buffers with max size
    const positions = new Float32Array(MAX_POINTS * 3); // x, y, z for each point
    const colors = new Float32Array(MAX_POINTS * 3);    // r, g, b for each point
    
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
    
    // Set initial draw range to 0 (no points visible yet)
    geometry.setDrawRange(0, 0);
    
    // Create material for points
    const material = new THREE.PointsMaterial({
        size: 0.5,
        vertexColors: true,
        transparent: true,
        opacity: 0.8
    });
    
    // Create the point cloud mesh
    points = new THREE.Points(geometry, material);
    scene.add(points);
    
    // Add ambient light
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
    scene.add(ambientLight);
    
    // Add grid helper for reference
    const gridHelper = new THREE.GridHelper(100, 20, 0x444444, 0x222222);
    scene.add(gridHelper);
    
    // Handle window resize
    window.addEventListener('resize', onWindowResize, false);
    
    // Setup controls
    setupControls();
    
    // Start animation loop
    animate();
}

// Handle window resize
function onWindowResize() {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
}

// Animation loop
function animate() {
    requestAnimationFrame(animate);
    
    // Rotate the point cloud for better visualization
    if (points) {
        points.rotation.y += 0.001;
    }
    
    // Update FPS counter
    frameCount++;
    const currentTime = Date.now();
    if (currentTime - lastTime >= 1000) {
        document.getElementById('fps').textContent = frameCount;
        frameCount = 0;
        lastTime = currentTime;
    }
    
    renderer.render(scene, camera);
}

// Simulate incoming point cloud data frame
function generatePointCloudFrame() {
    // This simulates a frame of point cloud data
    // Replace this with your actual data source (IPC, WebSocket, etc.)
    const pointsPerFrame = 100;
    const frame = [];
    
    for (let i = 0; i < pointsPerFrame; i++) {
        frame.push({
            x: (Math.random() - 0.5) * 50,
            y: (Math.random() - 0.5) * 50,
            z: (Math.random() - 0.5) * 50,
            r: Math.random(),
            g: Math.random(),
            b: Math.random()
        });
    }
    
    return frame;
}

// Update point cloud with new frame data
function updatePointCloud(frameData) {
    const positions = geometry.attributes.position.array;
    const colors = geometry.attributes.color.array;
    
    frameData.forEach((point, i) => {
        if (currentPointCount >= MAX_POINTS) {
            // Implement a circular buffer - overwrite oldest points
            currentPointCount = 0;
        }
        
        const idx = currentPointCount * 3;
        
        // Update position
        positions[idx] = point.x;
        positions[idx + 1] = point.y;
        positions[idx + 2] = point.z;
        
        // Update color
        colors[idx] = point.r;
        colors[idx + 1] = point.g;
        colors[idx + 2] = point.b;
        
        currentPointCount++;
    });
    
    // Tell Three.js to update the geometry
    geometry.attributes.position.needsUpdate = true;
    geometry.attributes.color.needsUpdate = true;
    
    // Update draw range to show all points
    geometry.setDrawRange(0, currentPointCount);
    
    // Update UI
    document.getElementById('point-count').textContent = currentPointCount;
}

// Start streaming point cloud data
function startStream() {
    if (isStreaming) return;
    
    isStreaming = true;
    streamInterval = setInterval(() => {
        const frame = generatePointCloudFrame();
        updatePointCloud(frame);
    }, 50); // 20 FPS data stream
    
    console.log('Stream started');
}

// Stop streaming
function stopStream() {
    if (!isStreaming) return;
    
    isStreaming = false;
    clearInterval(streamInterval);
    console.log('Stream stopped');
}

// Clear all points
function clearPoints() {
    currentPointCount = 0;
    geometry.setDrawRange(0, 0);
    document.getElementById('point-count').textContent = '0';
}

// Setup UI controls
function setupControls() {
    document.getElementById('start-btn').addEventListener('click', startStream);
    document.getElementById('stop-btn').addEventListener('click', stopStream);
    document.getElementById('clear-btn').addEventListener('click', clearPoints);
    
    // Mouse controls for camera
    let isDragging = false;
    let previousMousePosition = { x: 0, y: 0 };
    
    renderer.domElement.addEventListener('mousedown', (e) => {
        isDragging = true;
        previousMousePosition = { x: e.clientX, y: e.clientY };
    });
    
    renderer.domElement.addEventListener('mousemove', (e) => {
        if (!isDragging) return;
        
        const deltaX = e.clientX - previousMousePosition.x;
        const deltaY = e.clientY - previousMousePosition.y;
        
        camera.position.x += deltaX * 0.05;
        camera.position.y -= deltaY * 0.05;
        
        previousMousePosition = { x: e.clientX, y: e.clientY };
    });
    
    renderer.domElement.addEventListener('mouseup', () => {
        isDragging = false;
    });
    
    // Mouse wheel for zoom
    renderer.domElement.addEventListener('wheel', (e) => {
        e.preventDefault();
        camera.position.z += e.deltaY * 0.05;
        camera.position.z = Math.max(10, Math.min(200, camera.position.z));
    });
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}

// Export functions for Electron IPC if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        updatePointCloud,
        startStream,
        stopStream,
        clearPoints
    };
}