'use client';
import React, { useEffect, useState } from 'react';
import { ScatterChart, Scatter, XAxis, YAxis, CartesianGrid, Tooltip, ReferenceDot } from 'recharts';
import * as signalR from '@microsoft/signalr';

type PersonPoint = {
  x: number;
  y: number;
  tid: number;
  status: string;
};

// new person point comes in:
// store em somewhere in a dynamic thing
// 

// Global or module-level map to store TID → color
const tidColorMap = new Map<number, string>();

function getLatestPoints(points: PersonPoint[]): PersonPoint[] {
  const latestMap = new Map<number, PersonPoint>();
  for (const point of points) {
    latestMap.set(point.tid, point); // Assumes newest data overwrites older
  }
  return Array.from(latestMap.values());
}
 
// Function to generate and assign a consistent color per TID
function getColorForTid(tid: number): string {
  if (!tidColorMap.has(tid)) {
    const hue = Math.floor(Math.random() * 360);         // Hue: 0–359
    const saturation = 80 + Math.random() * 20;           // Saturation: 80–100%
    const lightness = 50 + Math.random() * 20;            // Lightness: 50–70%
    const color = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
    tidColorMap.set(tid, color);
  }
  return tidColorMap.get(tid)!;
}

export default function ScatterChartComponent() {
  const [points, setPoints] = useState<PersonPoint[]>([]);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7187/web') // Your SignalR hub
      .withAutomaticReconnect()
      .build();

    connection.start().then(() => {
      console.log('Connected to SignalR');
    });

    connection.on('OnUnfilteredPoints', (data: PersonPoint[]) => {
      console.log('New data for raw visualizer!');
      setPoints(data);
      console.log('Current points state:', points);
    });

    return () => {
      connection.stop();
    };
  }, []);

  return (
    <div className="p-4">
      <ScatterChart width={600} height={400}>
        <CartesianGrid />
        <XAxis type="number" dataKey="x" domain={[-5, 5]} ticks={[-5, 0, 5]} />
        <YAxis type="number" dataKey="y" domain={[0, 10]} ticks={[0, 5, 10]} />
        <Tooltip />
<Scatter name="People" data={points} shape={(props: { cx?: number; cy?: number; payload?: PersonPoint }) => {
  const { cx = 0, cy = 0, payload } = props;
  const color = getColorForTid(payload?.tid ?? -1);

  return (
    <circle
      cx={cx}
      cy={cy}
      r={5}
      fill={color}
      stroke="black"
      strokeWidth={1}
    />
  );
}} />
  <ReferenceDot
    x={0}
    y={0}
    r={20} // This controls the radius of your half-circle
    shape={(props) => {
      const { cx, cy, r } = props;
      // SVG path for a half-circle
      return (
        <path
          d={`M ${cx-r} ${cy} A ${r} ${r} 0 0 1 ${cx+r} ${cy} Z`}
          fill="rgba(255, 0, 0, 0.2)"
          stroke="red"
        />
      );
    }}
  />
</ScatterChart>

<div className="mt-4">
  <h3 className="text-lg font-bold mb-2">Legend</h3>
  <ul>
    {getLatestPoints(points).map((pt) => (
      <li key={pt.tid} className="flex items-center space-x-2 mb-1">
        <div
          className="w-4 h-4 rounded"
          style={{ backgroundColor: getColorForTid(pt.tid) }}
        />
        <span className="text-sm">TID {pt.tid} – Status: {pt.status}</span>
      </li>
    ))}
  </ul>
</div>

    </div>
  );
}
