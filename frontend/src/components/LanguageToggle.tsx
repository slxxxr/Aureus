import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";

export function LanguageToggle() {
  const { i18n, t } = useTranslation();

  const setLanguage = (language: "ru" | "en") => {
    void i18n.changeLanguage(language);
  };

  return (
    <div
      className="flex items-center rounded-md border border-border bg-muted p-1"
      aria-label={t("language.switchLabel")}
    >
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setLanguage("ru")}
        aria-label={t("language.ru")}
        className={i18n.language === "ru" ? "bg-background shadow-sm" : ""}
      >
        {t("language.ruShort")}
      </Button>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setLanguage("en")}
        aria-label={t("language.en")}
        className={i18n.language === "en" ? "bg-background shadow-sm" : ""}
      >
        {t("language.enShort")}
      </Button>
    </div>
  );
}
