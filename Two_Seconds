'use client';

import { useState, useEffect } from 'react';
import ScatterChartComponent from '../components/ScatterChartComponent';

export const dynamic = 'force-dynamic';

export default function Home() {
  const [activeTab, setActiveTab] = useState('graph');
  const [lastUpdated, setLastUpdated] = useState('');

  useEffect(() => {
    if (activeTab === 'graph') {
      const now = new Date();
      setLastUpdated(now.toLocaleString());
    }
  }, [activeTab]);

  return (
    <main
      className="min-h-screen bg-black bg-cover bg-center bg-no-repeat p-12 flex flex-col items-center relative"
      style={{
        backgroundImage: "url('https://f2.toyhou.se/file/f2-toyhou-se/images/100018560_HeP3kfdjJAx5JRU.png')",
      }}
    >
      {/* Tabs */}
      <div className="flex space-x-6 mb-8">
        <button
          onClick={() => setActiveTab('graph')}
          className={`px-8 py-4 text-xl rounded-t-2xl font-semibold transition ${
            activeTab === 'graph'
              ? 'bg-black bg-opacity-80 text-white'
              : 'bg-gray-700 bg-opacity-50 text-gray-300 hover:bg-opacity-70'
          }`}
        >
          Graph
        </button>
        <button
          onClick={() => setActiveTab('data')}
          className={`px-8 py-4 text-xl rounded-t-2xl font-semibold transition ${
            activeTab === 'data'
              ? 'bg-black bg-opacity-80 text-white'
              : 'bg-gray-700 bg-opacity-50 text-gray-300 hover:bg-opacity-70'
          }`}
        >
          Data
        </button>
      </div>

      {/* Tab Content */}
      <div className="bg-black bg-opacity-80 rounded-2xl p-10 shadow-2xl w-full max-w-5xl flex flex-col items-center min-h-[60vh]">
        {activeTab === 'graph' ? (
          <>
            <ScatterChartComponent />
            <div className="mt-6 bg-gray-800 bg-opacity-60 rounded-lg px-6 py-3 text-sm text-gray-200 shadow-md">
              Last updated at: {lastUpdated}
            </div>
          </>
        ) : (
          <>
            <div className="w-full h-full border-2 border-gray-400 rounded-lg"></div>
            <div className="mt-6 flex space-x-4">
              <button className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition">
                Save as .xlsx
              </button>
              <button className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition">
                Save as .pdf
              </button>
            </div>
          </>
        )}
      </div>

      {/* Pagedoll using standard <img> */}
      <a
        href="https://www.youtube.com/watch?v=yzgS61zgPEg"
        target="_blank"
        rel="noopener noreferrer"
        className="fixed bottom-4 right-4 z-50"
      >
        <img
          src="https://f2.toyhou.se/file/f2-toyhou-se/images/99927551_mWP5pszNT02gzlV.png"
          alt="Pagedoll"
          className="w-24 h-auto hover:scale-105 transition-transform duration-300"
        />
      </a>
    </main>
  );
}
