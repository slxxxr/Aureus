// Keep in sync with backend: Aureus.UseCases/Validation/InputLimits.cs
export const InputLimits = {
  emailMaxLength: 254,
  passwordMaxLength: 128,

  workspaceNameMaxLength: 120,
  categoryNameMaxLength: 120,
  accountNameMaxLength: 120,

  transactionNameMaxLength: 200,
  transactionNoteMaxLength: 500,
  insightsQuestionMaxLength: 500,
} as const;
