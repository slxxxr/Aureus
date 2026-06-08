import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { App } from "@/app/App";
import { AuthProvider } from "@/features/auth/AuthContext";
import { WorkspaceProvider } from "@/features/workspaces/WorkspaceContext";
import { DashboardFiltersProvider } from "@/features/analytics/DashboardFiltersContext";
import "@/i18n";
import "@/index.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1 },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <WorkspaceProvider>
          <DashboardFiltersProvider>
            <App />
          </DashboardFiltersProvider>
        </WorkspaceProvider>
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>,
);
