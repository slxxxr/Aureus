import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import { presetRange, customRange, type DateRange, type PeriodPreset } from "@/features/analytics/period";

type DashboardFiltersValue = {
  preset: PeriodPreset | "custom";
  customFrom: string;
  customTo: string;
  range: DateRange;
  selectPreset: (preset: PeriodPreset) => void;
  changeCustom: (from: string, to: string) => void;
  accountIds: string[];
  setAccountIds: (ids: string[]) => void;
  categoryIds: string[];
  setCategoryIds: (ids: string[]) => void;
};

const DashboardFiltersContext = createContext<DashboardFiltersValue | undefined>(undefined);

export function DashboardFiltersProvider({ children }: { children: ReactNode }) {
  const { activeWorkspace } = useWorkspace();

  const [preset, setPreset] = useState<PeriodPreset | "custom">("month");
  const [customFrom, setCustomFrom] = useState("");
  const [customTo, setCustomTo] = useState("");
  const [accountIds, setAccountIds] = useState<string[]>([]);
  const [categoryIds, setCategoryIds] = useState<string[]>([]);

  const workspaceId = activeWorkspace?.id;
  const previousWorkspaceId = useRef(workspaceId);
  useEffect(() => {
    if (previousWorkspaceId.current !== workspaceId) {
      previousWorkspaceId.current = workspaceId;
      setAccountIds([]);
      setCategoryIds([]);
      setPreset("month");
      setCustomFrom("");
      setCustomTo("");
    }
  }, [workspaceId]);

  const range = useMemo(
    () => (preset === "custom" ? customRange(customFrom, customTo) : presetRange(preset)),
    [preset, customFrom, customTo],
  );

  const value = useMemo<DashboardFiltersValue>(
    () => ({
      preset,
      customFrom,
      customTo,
      range,
      selectPreset: (next) => {
        setPreset(next);
        setCustomFrom("");
        setCustomTo("");
      },
      changeCustom: (from, to) => {
        setCustomFrom(from);
        setCustomTo(to);
        if (from && to) setPreset("custom");
      },
      accountIds,
      setAccountIds,
      categoryIds,
      setCategoryIds,
    }),
    [preset, customFrom, customTo, range, accountIds, categoryIds],
  );

  return <DashboardFiltersContext.Provider value={value}>{children}</DashboardFiltersContext.Provider>;
}

export function useDashboardFilters(): DashboardFiltersValue {
  const context = useContext(DashboardFiltersContext);

  if (context === undefined) {
    throw new Error("useDashboardFilters must be used within a DashboardFiltersProvider");
  }

  return context;
}
