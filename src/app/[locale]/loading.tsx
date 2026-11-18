export default function Loading() {
  return (
    <div className="page-wrap">
      <div className="skeleton heading-skeleton" />
      <div className="product-grid">
        {[1, 2, 3, 4].map((x) => (
          <div key={x} className="skeleton product-skeleton" />
        ))}
      </div>
    </div>
  );
}
