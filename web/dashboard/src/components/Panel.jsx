export default function Panel({ title, subtitle, children, className = '' }) {
  return (
    <section className={`card ${className}`}>
      <header className="panel-header">
        <h2>{title}</h2>
        {subtitle && <p className="panel-subtitle">{subtitle}</p>}
      </header>
      {children}
    </section>
  );
}
