using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderProcessor.Application.Contracts.Email;
using OrderProcessor.Application.Contracts.Pricing;
using OrderProcessor.Application.Contracts.Repositories;
using OrderProcessor.Application.Contracts.Services;
using OrderProcessor.Application.Services;
using OrderProcessor.Infrastructure.Notifications;
using OrderProcessor.Infrastructure.Payments;
using OrderProcessor.Infrastructure.Persistence;

namespace OrderProcessor.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PaymentOptions>(
                configuration.GetSection(
                    PaymentOptions.SectionName));

            services.Configure<EmailOptions>(
              configuration.GetSection(
                  EmailOptions.SectionName));

            services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

            services.AddScoped<
                ISchoolRepository,
                SchoolRepository>();

            services.AddScoped<
                IProductRepository,
                ProductRepository>();

            services.AddScoped<
                IInventoryRepository,
                InventoryRepository>();

            services.AddScoped<
                IPricingService,
                PricingService>();

            services.AddScoped<
                IOrderProcessor,
                OrderProcessorService>();


            services.AddScoped<
                IEmailSender,
                SmtpEmailSender>();

            services.AddScoped<
                IEmailServices,
                EmailServices>();

            services.AddHttpClient<
                IPaymentClient,
                PaymentClient>((sp, client) =>
                {
                    var options =
                    sp.GetRequiredService<
                        IOptions<PaymentOptions>>().Value;

                    client.BaseAddress =
                    new Uri(options.BaseUrl);
                });

            return services;
        }
    }
}