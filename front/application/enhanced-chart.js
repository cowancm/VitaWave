// enhanced-chart.js - Professional Chart System
class ChartManager {
    constructor(canvasId, legendId) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas.getContext('2d');
        this.legendEl = document.getElementById(legendId);
        
        this.config = {
            padding: 70,
            xMin: -5,
            xMax: 5,
            yMin: 0,
            yMax: 10,
            width: this.canvas.width,
            height: this.canvas.height,
            colors: {
                grid: '#334155',
                axis: '#475569',
                text: '#cbd5e1',
                halfCircle: 'rgba(239, 68, 68, 0.15)',
                halfCircleStroke: '#ef4444'
            }
        };
        
        this.points = [];
        this.tidColorMap = new Map();
        this.lastUpdateTime = Date.now();
        this.updateCount = 0;
        
        this.setupCanvas();
        this.render();
    }
    
    setupCanvas() {
        // Handle high DPI displays
        const dpr = window.devicePixelRatio || 1;
        const rect = this.canvas.getBoundingClientRect();
        
        this.canvas.width = rect.width * dpr;
        this.canvas.height = rect.height * dpr;
        this.ctx.scale(dpr, dpr);
        
        this.config.width = rect.width;
        this.config.height = rect.height;
    }
    
    generateColorForTid(tid) {
        if (!this.tidColorMap.has(tid)) {
            const hue = (tid * 137.508) % 360; // Golden angle for better distribution
            const saturation = 70 + (tid % 20);
            const lightness = 55 + (tid % 15);
            const color = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
            this.tidColorMap.set(tid, color);
        }
        return this.tidColorMap.get(tid);
    }
    
    dataToCanvas(x, y) {
        const { padding, xMin, xMax, yMin, yMax, width, height } = this.config;
        const plotWidth = width - 2 * padding;
        const plotHeight = height - 2 * padding;
        
        const canvasX = padding + ((x - xMin) / (xMax - xMin)) * plotWidth;
        const canvasY = height - padding - ((y - yMin) / (yMax - yMin)) * plotHeight;
        
        return { canvasX, canvasY };
    }
    
    drawGrid() {
        const { padding, xMin, xMax, yMin, yMax, width, height, colors } = this.config;
        
        this.ctx.strokeStyle = colors.grid;
        this.ctx.lineWidth = 1;
        this.ctx.fillStyle = colors.text;
        this.ctx.font = '12px Inter, sans-serif';
        this.ctx.textAlign = 'center';
        
        // X-axis grid and labels
        const xStep = (xMax - xMin) / 10;
        for (let x = xMin; x <= xMax; x += xStep) {
            const { canvasX } = this.dataToCanvas(x, yMin);
            
            this.ctx.beginPath();
            this.ctx.moveTo(canvasX, padding);
            this.ctx.lineTo(canvasX, height - padding);
            this.ctx.stroke();
            
            this.ctx.fillText(x.toFixed(1), canvasX, height - padding + 25);
        }
        
        // Y-axis grid and labels
        const yStep = (yMax - yMin) / 10;
        this.ctx.textAlign = 'right';
        for (let y = yMin; y <= yMax; y += yStep) {
            const { canvasY } = this.dataToCanvas(xMin, y);
            
            this.ctx.beginPath();
            this.ctx.moveTo(padding, canvasY);
            this.ctx.lineTo(width - padding, canvasY);
            this.ctx.stroke();
            
            this.ctx.fillText(y.toFixed(1), padding - 15, canvasY + 4);
        }
        
        // Axis lines
        this.ctx.strokeStyle = colors.axis;
        this.ctx.lineWidth = 2;
        
        // X-axis
        this.ctx.beginPath();
        this.ctx.moveTo(padding, height - padding);
        this.ctx.lineTo(width - padding, height - padding);
        this.ctx.stroke();
        
        // Y-axis
        this.ctx.beginPath();
        this.ctx.moveTo(padding, padding);
        this.ctx.lineTo(padding, height - padding);
        this.ctx.stroke();
        
        // Axis labels
        this.ctx.fillStyle = colors.text;
        this.ctx.font = 'bold 14px Inter, sans-serif';
        this.ctx.textAlign = 'center';
        this.ctx.fillText('X Position (meters)', width / 2, height - 10);
        
        this.ctx.save();
        this.ctx.translate(20, height / 2);
        this.ctx.rotate(-Math.PI / 2);
        this.ctx.fillText('Y Position (meters)', 0, 0);
        this.ctx.restore();
    }
    
    drawHalfCircle() {
        const radius = 50;
        const { canvasX, canvasY } = this.dataToCanvas(0, 0);
        const { colors } = this.config;
        
        this.ctx.fillStyle = colors.halfCircle;
        this.ctx.strokeStyle = colors.halfCircleStroke;
        this.ctx.lineWidth = 2;
        
        this.ctx.beginPath();
        this.ctx.arc(canvasX, canvasY, radius, 0, Math.PI, true);
        this.ctx.closePath();
        this.ctx.fill();
        this.ctx.stroke();
    }
    
    drawPoints() {
        this.points.forEach((point, index) => {
            const { canvasX, canvasY } = this.dataToCanvas(point.x, point.y);
            const color = this.generateColorForTid(point.tid);
            
            // Shadow for depth
            this.ctx.shadowColor = 'rgba(0, 0, 0, 0.3)';
            this.ctx.shadowBlur = 4;
            this.ctx.shadowOffsetX = 2;
            this.ctx.shadowOffsetY = 2;
            
            // Point
            this.ctx.fillStyle = color;
            this.ctx.strokeStyle = '#1e293b';
            this.ctx.lineWidth = 2;
            
            this.ctx.beginPath();
            this.ctx.arc(canvasX, canvasY, 6, 0, 2 * Math.PI);
            this.ctx.fill();
            this.ctx.stroke();
            
            // Reset shadow
            this.ctx.shadowColor = 'transparent';
            this.ctx.shadowBlur = 0;
            this.ctx.shadowOffsetX = 0;
            this.ctx.shadowOffsetY = 0;
        });
    }
    
    updateLegend() {
        const latestPoints = this.getLatestPointsByTid();
        
        this.legendEl.innerHTML = latestPoints.map(point => {
            const color = this.generateColorForTid(point.tid);
            return `
                <div class="legend-item">
                    <div class="legend-color" style="background-color: ${color}"></div>
                    <div class="legend-text">
                        <div class="legend-tid">Participant ${point.tid}</div>
                        <div class="legend-status">${point.status || 'Active'}</div>
                    </div>
                </div>
            `;
        }).join('');
    }
    
    getLatestPointsByTid() {
        const latestMap = new Map();
        this.points.forEach(point => {
            latestMap.set(point.tid, point);
        });
        return Array.from(latestMap.values());
    }
    
    updateStats() {
    const uniqueTids = new Set(this.points.map(p => p.tid)).size;
    
    // Safely update stats if elements exist
    const totalParticipants = document.getElementById('totalParticipants');
    const totalDataPoints = document.getElementById('totalDataPoints');
    const activeSessions = document.getElementById('activeSessions');
    const updateRate = document.getElementById('updateRate');
    
    if (totalParticipants) totalParticipants.textContent = uniqueTids;
    if (totalDataPoints) totalDataPoints.textContent = this.points.length;
    if (activeSessions) activeSessions.textContent = this.getLatestPointsByTid().length;
    
    // Calculate update rate
    const now = Date.now();
    const elapsed = (now - this.lastUpdateTime) / 1000;
    if (elapsed > 0 && updateRate) {
        const rate = (1 / elapsed).toFixed(2);
        updateRate.textContent = `${rate} Hz`;
    }
    this.lastUpdateTime = now;
    }
    
    render() {
        this.ctx.clearRect(0, 0, this.config.width, this.config.height);
        this.drawGrid();
        this.drawHalfCircle();
        this.drawPoints();
        this.updateLegend();
        this.updateStats();
    }
    
    setPoints(newPoints) {
        this.points = newPoints;
        this.render();
        this.updateCount++;
    }
    
    addPoint(point) {
        this.points.push(point);
        this.render();
    }
    
    clearPoints() {
        this.points = [];
        this.render();
    }
}

// Initialize chart
// Initialize chart and make it globally available
window.chartManager = null;

// Wait for DOM to be ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initChart);
} else {
    initChart();
}

function initChart() {
    console.log('=== Initializing Chart Manager ===');
    const canvas = document.getElementById('chartCanvas');
    const legend = document.getElementById('legendItems');
    
    if (!canvas || !legend) {
        console.error('Chart elements not found!');
        return;
    }
    
    window.chartManager = new ChartManager('chartCanvas', 'legendItems');
    console.log('✅ Chart Manager initialized and ready');
    
    // Added test data button functionality
    const testBtn = document.getElementById('testDataButton');
    if (testBtn) {
        testBtn.addEventListener('click', () => {
            console.log('Generating test data...');
            const testData = [
                { tid: 1, x: -2.5, y: 3.2, status: 'Active' },
                { tid: 2, x: 1.8, y: 5.7, status: 'Active' },
                { tid: 3, x: -1.2, y: 7.3, status: 'Active' },
                { tid: 4, x: 3.4, y: 2.1, status: 'Active' },
                { tid: 5, x: 0.5, y: 8.9, status: 'Active' }
            ];
            window.chartManager.setPoints(testData);
            console.log('✅ Test data added to chart');
        });
    }
}