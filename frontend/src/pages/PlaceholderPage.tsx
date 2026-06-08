import { useTranslation } from "react-i18next";

type PlaceholderPageProps = {
  titleKey: string;
  descriptionKey: string;
};

export function PlaceholderPage({ titleKey, descriptionKey }: PlaceholderPageProps) {
  const { t } = useTranslation();

  return (
    <section className="mx-auto max-w-5xl pt-4">
      <div className="border-b border-border pb-5">
        <h2 className="text-2xl font-semibold tracking-normal">{t(titleKey)}</h2>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">{t(descriptionKey)}</p>
      </div>

      <div className="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div className="rounded-md border border-border bg-background p-4">
          <p className="text-xs font-medium uppercase text-muted-foreground">{t("placeholder.statusLabel")}</p>
          <p className="mt-2 text-sm">{t("placeholder.statusValue")}</p>
        </div>
        <div className="rounded-md border border-border bg-background p-4">
          <p className="text-xs font-medium uppercase text-muted-foreground">{t("placeholder.scopeLabel")}</p>
          <p className="mt-2 text-sm">{t("placeholder.scopeValue")}</p>
        </div>
        <div className="rounded-md border border-border bg-background p-4 sm:col-span-2 lg:col-span-1">
          <p className="text-xs font-medium uppercase text-muted-foreground">{t("placeholder.currencyLabel")}</p>
          <p className="mt-2 text-sm">{t("placeholder.currencyValue")}</p>
        </div>
      </div>
    </section>
  );
}
