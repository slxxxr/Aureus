import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Send, Sparkles, X } from "lucide-react";
import ReactMarkdown from "react-markdown";
import { Button } from "@/components/ui/button";
import { askInsights } from "@/features/analytics/analyticsApi";

const LANGUAGE_MAP: Record<string, string> = {
  ru: "Russian",
  en: "English",
};

interface Props {
  workspaceId: string;
  from?: string;
  to?: string;
}

export function InsightsCard({ workspaceId, from, to }: Props) {
  const { t, i18n } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [question, setQuestion] = useState("");

  const language = LANGUAGE_MAP[i18n.language] ?? "English";

  const { mutate, data, isPending, isError } = useMutation({
    mutationFn: () => askInsights(workspaceId, question.trim(), from, to, language),
  });

  const canSubmit = question.trim().length > 0 && !isPending;

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (canSubmit) mutate();
  }

  if (!isOpen) {
    return (
      <button
        type="button"
        onClick={() => setIsOpen(true)}
        className="flex w-full items-center gap-2 rounded-lg border border-border bg-card px-3 py-2.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
      >
        <Sparkles className="h-3.5 w-3.5 shrink-0" />
        <span>{t("dashboard.insights.title")}</span>
      </button>
    );
  }

  return (
    <div className="rounded-lg border border-border bg-card">
      <div className="flex items-center gap-2 border-b border-border px-3 py-2.5">
        <Sparkles className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        <span className="flex-1 text-sm font-medium">{t("dashboard.insights.title")}</span>
        <button
          type="button"
          onClick={() => setIsOpen(false)}
          className="text-muted-foreground transition-colors hover:text-foreground"
        >
          <X className="h-3.5 w-3.5" />
        </button>
      </div>

      <div className="p-3">
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="flex gap-2">
            <textarea
              autoComplete="off"
              placeholder={t("dashboard.insights.placeholder")}
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  if (canSubmit) mutate();
                }
              }}
              rows={2}
              className="flex-1 resize-none rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            />
            <Button type="submit" size="icon" variant="ghost" disabled={!canSubmit} className="self-end shrink-0">
              <Send className="h-4 w-4" />
            </Button>
          </div>

          {isPending && (
            <p className="animate-pulse text-sm text-muted-foreground">{t("dashboard.insights.loading")}</p>
          )}

          {isError && (
            <p className="text-sm text-destructive">{t("dashboard.insights.error")}</p>
          )}

          {data && (
            <div className="rounded-md border bg-muted/40 px-4 py-3 text-sm leading-relaxed [&_h1]:font-semibold [&_h2]:font-semibold [&_h3]:font-semibold [&_li]:mb-0.5 [&_ol]:mb-2 [&_ol]:list-decimal [&_ol]:pl-4 [&_p]:mb-2 [&_strong]:font-semibold [&_ul]:mb-2 [&_ul]:list-disc [&_ul]:pl-4">
              <ReactMarkdown>{data.answer}</ReactMarkdown>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
