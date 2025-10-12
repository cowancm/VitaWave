import Plotly from 'plotly.js-dist-min';

// Create random data for testing
const N = 2000;
const x = Array.from({ length: N }, () => Math.random() * 10 - 5);
const y = Array.from({ length: N }, () => Math.random() * 10 - 5);
const z = Array.from({ length: N }, () => Math.random() * 10 - 5);

const trace = {
  x, y, z,
  mode: 'markers',
  type: 'scatter3d',
  marker: {
    size: 3,
    color: z,
    colorscale: 'Viridis',
    opacity: 0.8,
  },
};

const layout = {
  margin: { l: 0, r: 0, b: 0, t: 0 },
  scene: {
    xaxis: { title: 'X' },
    yaxis: { title: 'Y' },
    zaxis: { title: 'Z' },
  },
};

// render it once DOM is ready
window.addEventListener('DOMContentLoaded', () => {
  Plotly.newPlot('chart', [trace], layout, { displayModeBar: false });

  // Optional: dynamically update points
  setInterval(() => {
    for (let i = 0; i < N; i++) {
      x[i] += (Math.random() - 0.5) * 0.2;
      y[i] += (Math.random() - 0.5) * 0.2;
      z[i] += (Math.random() - 0.5) * 0.2;
    }
    Plotly.update('chart', { x: [x], y: [y], z: [z] });
  }, 2000);
});
