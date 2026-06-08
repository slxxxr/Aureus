import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  BarChart3,
  CreditCard,
  FolderTree,
  LogOut,
  Menu,
  PanelLeftClose,
  PanelLeftOpen,
  ReceiptText,
  Settings,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { LanguageToggle } from "@/components/LanguageToggle";
import { WorkspaceSwitcher } from "@/components/WorkspaceSwitcher";
import { useAuth } from "@/features/auth/AuthContext";
import { cn } from "@/lib/utils";

type NavigationItem = {
  to: string;
  labelKey: string;
  icon: typeof BarChart3;
  end?: boolean;
};

const navigation: NavigationItem[] = [
  { to: "/", labelKey: "navigation.dashboard", icon: BarChart3, end: true },
  { to: "/accounts", labelKey: "navigation.accounts", icon: CreditCard },
  { to: "/transactions", labelKey: "navigation.transactions", icon: ReceiptText },
  { to: "/categories", labelKey: "navigation.categories", icon: FolderTree },
  { to: "/settings", labelKey: "navigation.settings", icon: Settings },
];

const pageTitleByPath: Record<string, string> = {
  "/": "pages.dashboard.title",
  "/accounts": "pages.accounts.title",
  "/transactions": "pages.transactions.title",
  "/categories": "pages.categories.title",
  "/settings": "pages.settings.title",
};

const SIDEBAR_COLLAPSED_KEY = "sidebar-collapsed";
const SIDEBAR_WIDTH_KEY = "sidebar-width";
const MIN_WIDTH = 180;
const MAX_WIDTH = 400;
const COLLAPSED_WIDTH = 56;
const DEFAULT_WIDTH = 256;

export function AppLayout() {
  const { t } = useTranslation();
  const location = useLocation();
  const { signOut } = useAuth();
  const currentTitleKey = pageTitleByPath[location.pathname] ?? "pages.dashboard.title";

  const [isCollapsed, setIsCollapsed] = useState(
    () => localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === "true",
  );
  const [sidebarWidth, setSidebarWidth] = useState(() => {
    const stored = localStorage.getItem(SIDEBAR_WIDTH_KEY);
    return stored
      ? Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, parseInt(stored, 10)))
      : DEFAULT_WIDTH;
  });
  const [isResizing, setIsResizing] = useState(false);

  const dragState = useRef({ dragging: false, startX: 0, startWidth: 0 });

  const effectiveWidth = isCollapsed ? COLLAPSED_WIDTH : sidebarWidth;

  useEffect(() => {
    const onMouseMove = (e: MouseEvent) => {
      if (!dragState.current.dragging) return;
      const delta = e.clientX - dragState.current.startX;
      const newWidth = Math.min(
        MAX_WIDTH,
        Math.max(MIN_WIDTH, dragState.current.startWidth + delta),
      );
      setSidebarWidth(newWidth);
    };

    const onMouseUp = () => {
      if (!dragState.current.dragging) return;
      dragState.current.dragging = false;
      setIsResizing(false);
      setSidebarWidth((prev) => {
        localStorage.setItem(SIDEBAR_WIDTH_KEY, String(prev));
        return prev;
      });
    };

    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", onMouseUp);
    return () => {
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    };
  }, []);

  const handleDragStart = (e: React.MouseEvent) => {
    dragState.current = { dragging: true, startX: e.clientX, startWidth: sidebarWidth };
    setIsResizing(true);
    e.preventDefault();
  };

  const toggleCollapse = () => {
    setIsCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(next));
      return next;
    });
  };

  const sidebarTransition = isResizing ? "none" : "width 200ms ease";

  return (
    <div className="flex h-screen overflow-hidden bg-background text-foreground">
      {/* Spacer that matches sidebar width — pushes content right on desktop */}
      <div
        className="hidden shrink-0 md:block"
        style={{ width: effectiveWidth, transition: sidebarTransition }}
      />

      {/* Fixed sidebar */}
      <aside
        className="fixed inset-y-0 left-0 hidden border-r border-border bg-muted/40 py-4 md:flex md:flex-col"
        style={{ width: effectiveWidth, transition: sidebarTransition }}
      >
        <div className={cn("mb-6 flex items-center", isCollapsed ? "h-12 justify-center px-2" : "px-3")}>
          <WorkspaceSwitcher collapsed={isCollapsed} />
        </div>

        <nav
          className={cn("flex-1 space-y-1 overflow-y-auto", isCollapsed ? "px-2" : "px-3")}
          aria-label={t("navigation.primaryLabel")}
        >
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              title={isCollapsed ? t(item.labelKey) : undefined}
              className={({ isActive }) =>
                cn(
                  "flex h-9 items-center rounded-md px-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground",
                  isActive && "bg-accent text-accent-foreground",
                  isCollapsed ? "justify-center" : "gap-3",
                )
              }
            >
              <item.icon className="h-4 w-4 shrink-0" aria-hidden="true" />
              {!isCollapsed && <span>{t(item.labelKey)}</span>}
            </NavLink>
          ))}
        </nav>

        {/* Collapse toggle — bottom, left-aligned */}
        <div className={cn("mt-auto pt-2", isCollapsed ? "px-2" : "px-3")}>
          <button
            type="button"
            onClick={toggleCollapse}
            className={cn(
              "flex h-8 items-center gap-3 rounded-md px-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground",
              isCollapsed ? "w-8 justify-center" : "w-full",
            )}
            aria-label={isCollapsed ? t("navigation.expandSidebar") : t("navigation.collapseSidebar")}
            title={isCollapsed ? t("navigation.expandSidebar") : undefined}
          >
            {isCollapsed ? (
              <PanelLeftOpen className="h-4 w-4 shrink-0" aria-hidden="true" />
            ) : (
              <>
                <PanelLeftClose className="h-4 w-4 shrink-0" aria-hidden="true" />
                <span>{t("navigation.collapseSidebar")}</span>
              </>
            )}
          </button>
        </div>

        {/* Drag handle — right edge, only when expanded */}
        {!isCollapsed && (
          <div
            onMouseDown={handleDragStart}
            className="absolute inset-y-0 right-0 w-1 cursor-ew-resize hover:bg-primary/20 active:bg-primary/30"
          />
        )}
      </aside>

      {/* Main content */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-background/95 px-4 backdrop-blur md:px-6">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="icon" className="md:hidden" aria-label={t("navigation.mobileMenuLabel")}>
              <Menu className="h-5 w-5" aria-hidden="true" />
            </Button>
            <div>
              <p className="text-xs text-muted-foreground md:hidden">{t("common.appName")}</p>
              <h1 className="text-base font-semibold md:text-lg">{t(currentTitleKey)}</h1>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <LanguageToggle />
            <Button
              variant="ghost"
              size="icon"
              onClick={signOut}
              aria-label={t("auth.signOut")}
              title={t("auth.signOut")}
            >
              <LogOut className="h-4 w-4" aria-hidden="true" />
            </Button>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto px-4 pb-4 md:px-8 md:pb-5">
          <Outlet />
        </main>

        <nav
          className="sticky bottom-0 grid grid-cols-5 border-t border-border bg-background md:hidden"
          aria-label={t("navigation.primaryLabel")}
        >
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  "flex h-14 flex-col items-center justify-center gap-1 text-[11px] text-muted-foreground",
                  isActive && "text-foreground",
                )
              }
            >
              <item.icon className="h-4 w-4" aria-hidden="true" />
              <span className="max-w-full truncate px-1">{t(item.labelKey)}</span>
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  );
}
