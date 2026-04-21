namespace BudgetTracker.Api.Features.Transactions.Import.Detection;

public static class ColumnMappingDictionary
{
    public static readonly string[] DateColumns =
        ["Date", "Transaction Date", "Posting Date", "Trans Date", "Value Date", "Settlement Date", "Data"];

    public static readonly string[] DescriptionColumns =
        ["Description", "Memo", "Details", "Narrative", "Reference", "Payee", "Transaction Description", "Merchant Name", "Descricao", "Descrição"];

    public static readonly string[] AmountColumns =
        ["Amount", "Transaction Amount", "Debit", "Credit", "Debit Amount", "Credit Amount", "Value", "Montante", "Valor"];

    public static readonly string[] BalanceColumns =
        ["Balance", "Running Balance", "Account Balance", "Available Balance", "Closing Balance", "Saldo"];

    public static readonly string[] CategoryColumns =
        ["Category", "Type", "Transaction Type", "Trans Type", "Categoria", "Tipo"];
}
