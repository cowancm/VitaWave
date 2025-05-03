import ScatterChartComponent from '../components/ScatterChartComponent';
export const dynamic = 'force-dynamic';
export default function Home() {
  return (
    <main className="flex min-h-screen items-center justify-center">
      <ScatterChartComponent />
    </main>
  );
}
