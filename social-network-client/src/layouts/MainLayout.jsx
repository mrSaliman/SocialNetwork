import Header from '../components/Header';

export default function MainLayout({ children, showHeader = true }) {
  return (
    <div className="flex flex-col min-h-screen">
      {showHeader && <Header />}
      <main className="flex-grow">
        {children}
      </main>
    </div>
  );
}