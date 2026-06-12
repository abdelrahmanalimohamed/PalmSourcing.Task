using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderProcessor.Application.Contracts.Services;
using OrderProcessor.Application.DTOs;
using OrderProcessor.Domain.Models;
using OrderProcessor.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration);

using var host = builder.Build();

var processor =
    host.Services.GetRequiredService<IOrderProcessor>();

var result =
    await processor.ProcessAsync(
        new ProcessOrderRequest(
            SchoolId: 1,
            ParentEmail: "parent@test.com",
            Lines:
            [
                new OrderLine(
                    "POLO-001",
                    2,
                    "ABC")
            ]));

Console.WriteLine(
    $"Success: {result.Success}");

Console.WriteLine(
    $"Message: {result.Message}");

Console.WriteLine(
    $"Total: {result.Total}");