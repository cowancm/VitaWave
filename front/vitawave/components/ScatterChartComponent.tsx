'use client';
import React, { useEffect, useState } from 'react';
import { ScatterChart, Scatter, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import * as signalR from '@microsoft/signalr';

type PersonPoint = {
  x: number;
  y: number;
  tid: number;
  status: string;
};

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
        <XAxis type="number" dataKey="x" domain={[-5, 5]} />
        <YAxis type="number" dataKey="y" domain={[0, 10]} />
        <Tooltip />
        <Scatter name="People" data={points} fill="#82ca9d" />
      </ScatterChart>
    </div>
  );
}
