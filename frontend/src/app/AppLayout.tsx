import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  BarChart3,
  CreditCard,
  FolderTree,
  Landmark,
  Menu,
  ReceiptText,
  Settings,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const navigation = [
  { to: "/", labelKey: "navigation.dashboard", icon: BarChart3, end: true },
  { to: "/accounts", labelKey: "navigation.accounts", icon: CreditCard },
  { to: "/transactions", labelKey: "navigation.transactions", icon: ReceiptText },
  { to: "/categories", labelKey: "navigation.categories", icon: FolderTree },
  { to: "/settings", labelKey: "navigation.settings", icon: Settings },
] as const;

const pageTitleByPath: Record<string, string> = {
  "/": "pages.dashboard.title",
  "/accounts": "pages.accounts.title",
  "/transactions": "pages.transactions.title",
  "/categories": "pages.categories.title",
  "/settings": "pages.settings.title",
};

export function AppLayout() {
  const { i18n, t } = useTranslation();
  const location = useLocation();
  const currentTitleKey = pageTitleByPath[location.pathname] ?? "pages.dashboard.title";

  const setLanguage = (language: "ru" | "en") => {
    void i18n.changeLanguage(language);
  };

  return (
    <div className="min-h-screen bg-background text-foreground">
      <aside className="fixed inset-y-0 left-0 hidden w-64 border-r border-border bg-muted/40 px-3 py-4 md:flex md:flex-col">
        <div className="mb-6 flex items-center gap-3 px-2">
          <div className="flex h-9 w-9 items-center justify-center rounded-md border border-border bg-background">
            <Landmark className="h-5 w-5" aria-hidden="true" />
          </div>
          <div>
            <p className="text-sm font-semibold leading-5">{t("common.appName")}</p>
            <p className="text-xs text-muted-foreground">{t("common.tagline")}</p>
          </div>
        </div>

        <nav className="space-y-1" aria-label={t("navigation.primaryLabel")}>
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  "flex h-9 items-center gap-3 rounded-md px-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground",
                  isActive && "bg-accent text-accent-foreground",
                )
              }
            >
              <item.icon className="h-4 w-4" aria-hidden="true" />
              <span>{t(item.labelKey)}</span>
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-h-screen flex-col md:pl-64">
        <header className="sticky top-0 z-10 flex h-14 items-center justify-between border-b border-border bg-background/95 px-4 backdrop-blur md:px-6">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="icon" className="md:hidden" aria-label={t("navigation.mobileMenuLabel")}>
              <Menu className="h-5 w-5" aria-hidden="true" />
            </Button>
            <div>
              <p className="text-xs text-muted-foreground md:hidden">{t("common.appName")}</p>
              <h1 className="text-base font-semibold md:text-lg">{t(currentTitleKey)}</h1>
            </div>
          </div>

          <div className="flex items-center rounded-md border border-border bg-muted p-1" aria-label={t("language.switchLabel")}>
            <Button
              variant={i18n.language === "ru" ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setLanguage("ru")}
              aria-label={t("language.ru")}
            >
              {t("language.ruShort")}
            </Button>
            <Button
              variant={i18n.language === "en" ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setLanguage("en")}
              aria-label={t("language.en")}
            >
              {t("language.enShort")}
            </Button>
          </div>
        </header>

        <main className="flex-1 px-4 py-5 md:px-8 md:py-8">
          <Outlet />
        </main>

        <nav className="sticky bottom-0 grid grid-cols-5 border-t border-border bg-background md:hidden" aria-label={t("navigation.primaryLabel")}>
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
