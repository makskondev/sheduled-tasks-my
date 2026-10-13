namespace CronTaskRunner.Sample.Deals;

public enum DiscrepancyKind
{
    /// <summary>Сделка есть не во всех источниках.</summary>
    MissingInSomeSources,

    /// <summary>Сделка есть везде, но поля (сумма/валюта/дата) расходятся.</summary>
    FieldMismatch,

    /// <summary>В каком-то источнике несколько записей с одним DealId.</summary>
    DuplicateInSource,
}
