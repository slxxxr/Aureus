import { InputLimits } from "@/lib/inputLimits";
import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import {
  createCategory,
  deleteCategory,
  getCategories,
  updateCategory,
  type Category,
  type CategoryType,
} from "@/features/categories/categoriesApi";
import { resolveCategoryError } from "@/features/categories/resolveCategoryError";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Modal } from "@/components/ui/modal";
// ─── create modal ─────────────────────────────────────────────────────────────

function CreateCategoryModal({
  workspaceId,
  defaultType,
  onClose,
}: {
  workspaceId: string;
  defaultType: CategoryType;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");

  const mutation = useMutation({
    mutationFn: () => createCategory(workspaceId, { name: name.trim(), type: defaultType }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["categories", workspaceId] });
      onClose();
    },
  });

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("categories.createModal.title")}</h2>
      <form onSubmit={(e: FormEvent) => { e.preventDefault(); mutation.mutate(); }} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="create-category-name">{t("categories.createModal.nameLabel")}</Label>
          <Input
            id="create-category-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("categories.createModal.namePlaceholder")}
            required
            autoFocus
            autoComplete="off"
            maxLength={InputLimits.categoryNameMaxLength}
            disabled={mutation.isPending}
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveCategoryError(mutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" disabled={mutation.isPending || !name.trim()}>
            {mutation.isPending ? t("categories.createModal.submitting") : t("categories.createModal.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// ─── edit modal ───────────────────────────────────────────────────────────────

function EditCategoryModal({
  category,
  workspaceId,
  onClose,
}: {
  category: Category;
  workspaceId: string;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [name, setName] = useState(category.name);
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["categories", workspaceId] });

  const updateMutation = useMutation({
    mutationFn: () => updateCategory(workspaceId, category.id, { name: name.trim() }),
    onSuccess: () => { void invalidate(); onClose(); },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteCategory(workspaceId, category.id),
    onSuccess: () => { void invalidate(); onClose(); },
  });

  const isPending = updateMutation.isPending || deleteMutation.isPending;

  if (confirmingDelete) {
    return (
      <Modal onBackdropClick={() => setConfirmingDelete(false)}>
        <h2 className="mb-2 text-base font-semibold">{t("categories.deleteConfirm.title")}</h2>
        <p className="mb-5 text-sm text-muted-foreground">{t("categories.deleteConfirm.description")}</p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setConfirmingDelete(false)} disabled={deleteMutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button
            disabled={deleteMutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            onClick={() => deleteMutation.mutate()}
          >
            {t("common.delete")}
          </Button>
        </div>
      </Modal>
    );
  }

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("categories.editModal.title")}</h2>
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); updateMutation.mutate(); }}
        className="space-y-4"
      >
        <div className="space-y-1.5">
          <Label htmlFor="edit-category-name">{t("categories.editModal.nameLabel")}</Label>
          <Input
            id="edit-category-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
            autoComplete="off"
            maxLength={InputLimits.categoryNameMaxLength}
            disabled={isPending}
          />
        </div>

        {updateMutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveCategoryError(updateMutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex items-center justify-between pt-1">
          <button
            type="button"
            onClick={() => setConfirmingDelete(true)}
            disabled={isPending}
            className="text-sm text-destructive hover:underline disabled:opacity-50"
          >
            {t("categories.editModal.deleteCategory")}
          </button>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isPending || !name.trim() || name.trim() === category.name}>
              {updateMutation.isPending ? t("categories.editModal.saving") : t("categories.editModal.save")}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// ─── category card ────────────────────────────────────────────────────────────

function CategoryCard({ category, onEdit }: { category: Category; onEdit: () => void }) {
  const { t } = useTranslation();

  return (
    <div className="group relative flex flex-col rounded-lg border border-border bg-card p-4 transition-shadow hover:shadow-sm">
      <div className="mb-3 flex items-start justify-between gap-2">
        <p className="text-sm font-medium leading-tight">{category.name}</p>
        <button
          onClick={onEdit}
          className="shrink-0 rounded p-0.5 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100 hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          aria-label={t("categories.editModal.title")}
        >
          <Pencil className="h-3.5 w-3.5" />
        </button>
      </div>

      {/* stats — placeholder until transactions feature */}
      <div className="mt-auto border-t border-border pt-3">
        <p className="text-lg font-semibold tabular-nums text-muted-foreground">—</p>
        <p className="mt-0.5 text-xs text-muted-foreground">{t("categories.statsLast30Days")}</p>
      </div>
    </div>
  );
}

// ─── section ──────────────────────────────────────────────────────────────────

function CategorySection({
  title,
  categories,
  onAdd,
  onEdit,
}: {
  title: string;
  categories: Category[];
  onAdd: () => void;
  onEdit: (c: Category) => void;
}) {
  const { t } = useTranslation();
  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</span>
        <Button size="sm" variant="ghost" onClick={onAdd} className="gap-1.5">
          <Plus className="h-3.5 w-3.5" aria-hidden="true" />
          {t("categories.addCategory")}
        </Button>
      </div>

      {categories.length === 0 ? (
        <p className="text-xs text-muted-foreground">{t("categories.emptySection")}</p>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {categories.map((cat) => (
            <CategoryCard key={cat.id} category={cat} onEdit={() => onEdit(cat)} />
          ))}
        </div>
      )}
    </div>
  );
}

// ─── skeleton ─────────────────────────────────────────────────────────────────

function CategoriesSkeleton() {
  return (
    <div className="space-y-8 animate-pulse">
      {[3, 2].map((count, i) => (
        <div key={i}>
          <div className="mb-3 h-3 w-16 rounded bg-muted" />
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {Array.from({ length: count }).map((_, j) => (
              <div key={j} className="rounded-lg border border-border bg-card p-4 space-y-3">
                <div className="h-3.5 w-20 rounded bg-muted" />
                <div className="h-4 w-12 rounded bg-muted" />
                <div className="border-t border-border pt-3 space-y-1.5">
                  <div className="h-5 w-8 rounded bg-muted" />
                  <div className="h-2.5 w-20 rounded bg-muted" />
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

// ─── page ─────────────────────────────────────────────────────────────────────

export function CategoriesPage() {
  const { activeWorkspace } = useWorkspace();
  const { t } = useTranslation();
  const [creating, setCreating] = useState<CategoryType | null>(null);
  const [editing, setEditing] = useState<Category | null>(null);

  const { data: categories, isLoading } = useQuery({
    queryKey: ["categories", activeWorkspace?.id],
    queryFn: () => getCategories(activeWorkspace!.id),
    enabled: activeWorkspace !== null,
    staleTime: 30_000,
  });

  const expenses = (categories ?? []).filter((c) => c.type === "Expense");
  const income = (categories ?? []).filter((c) => c.type === "Income");

  return (
    <div className="pt-9">
      {isLoading && <CategoriesSkeleton />}

      {!isLoading && (
        <div className="space-y-8">
          <CategorySection
            title={t("categories.expenseSection")}
            categories={expenses}
            onAdd={() => setCreating("Expense")}
            onEdit={setEditing}
          />
          <CategorySection
            title={t("categories.incomeSection")}
            categories={income}
            onAdd={() => setCreating("Income")}
            onEdit={setEditing}
          />
        </div>
      )}

      {creating && activeWorkspace && (
        <CreateCategoryModal
          workspaceId={activeWorkspace.id}
          defaultType={creating}
          onClose={() => setCreating(null)}
        />
      )}
      {editing && activeWorkspace && (
        <EditCategoryModal
          category={editing}
          workspaceId={activeWorkspace.id}
          onClose={() => setEditing(null)}
        />
      )}
    </div>
  );
}
