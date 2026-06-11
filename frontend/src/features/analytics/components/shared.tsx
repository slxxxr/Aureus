import { cn } from "@/lib/utils";

export function Section({ title, children }: { title?: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      {title && <h2 className="mb-3 text-sm font-semibold">{title}</h2>}
      {children}
    </div>
  );
}

export function DashboardSkeleton() {
  return (
    <div className="animate-pulse space-y-4">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {[0, 1, 2].map((i) => (
          <div key={i} className="h-20 rounded-lg border border-border bg-muted/40" />
        ))}
      </div>
      <div className="h-72 rounded-lg border border-border bg-muted/40" />
    </div>
  );
}
