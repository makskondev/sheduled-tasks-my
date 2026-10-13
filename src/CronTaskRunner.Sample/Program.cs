using CronTaskRunner.Core.Extensions;
using CronTaskRunner.Sample.Deals;
using CronTaskRunner.Sample.Email;
using CronTaskRunner.Sample.ExchangeRates;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Инфраструктура планировщика (секция "ScheduledEvents" из appsettings.json)
builder.Services.AddCronTaskRunner(builder.Configuration);

builder.Services.AddEmail(builder.Configuration);

// Задачи регистрируются под ключами, на которые ссылается TaskKey в конфиге
builder.Services.AddExchangeRateCheck(builder.Configuration);
builder.Services.AddDealsReconciliation(builder.Configuration);

await builder.Build().RunAsync();
