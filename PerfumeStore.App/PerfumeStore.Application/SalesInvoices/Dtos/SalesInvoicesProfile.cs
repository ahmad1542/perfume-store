using AutoMapper;
using PerfumeStore.Application.SalesInvoiceItems.Dtos;
using PerfumeStore.Application.SalesInvoices.Commands.CreateSalesInvoice;
using PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice;
using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Application.SalesInvoices.Dtos {
    public class SalesInvoicesProfile : Profile {
        public SalesInvoicesProfile() {
            CreateMap<SalesInvoice, SalesInvoiceDto>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : null))
                .ForMember(d => d.DebtAmount, opt => opt.MapFrom(s => s.Debt != null && !s.Debt.IsDeleted ? s.Debt.Amount : 0))
                .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.SalesInvoiceItems != null ? s.SalesInvoiceItems.Count : 0))
                .ForMember(d => d.ProductNames, opt => opt.MapFrom(s =>
                    s.SalesInvoiceItems != null
                        ? s.SalesInvoiceItems
                            .Where(i => i.Product != null && !string.IsNullOrWhiteSpace(i.Product.Name))
                            .Select(i => i.Product!.Name)
                            .Distinct()
                        : new List<string>() ));

            CreateMap<SalesInvoiceItem, SalesInvoiceItemDetailsDto>()
                .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : null));

            CreateMap<SalesInvoice, SalesInvoiceDetailsDto>()
                .ForMember(d => d.DebtNotes, opt => opt.MapFrom(s => s.Debt != null ? s.Debt.Notes : null))
                .ForMember(d => d.AppliedVoucherPayments, opt => opt.MapFrom(s => s.ReceiptVoucherSalesInvoices.Sum(v => v.AppliedAmount)))
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : null))
                .ForMember(d => d.DebtAmount, opt => opt.MapFrom(s => s.Debt != null ? s.Debt.Amount : 0))
                .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.SalesInvoiceItems.Count))
                .ForMember(d => d.Products, opt => opt.MapFrom(s => s.SalesInvoiceItems))
                .ForMember(d => d.DebtAmount, opt => opt.MapFrom(s => s.Debt != null && !s.Debt.IsDeleted ? s.Debt.Amount : 0));

            CreateMap<CreateSalesInvoiceCommand, SalesInvoice>()
                .ForMember(d => d.ID, opt => opt.Ignore())
                .ForMember(d => d.Customer, opt => opt.Ignore())
                .ForMember(d => d.ReceiptVoucherSalesInvoices, opt => opt.Ignore())
                .ForMember(d => d.SalesInvoiceItems, opt => opt.Ignore())
                .ForMember(d => d.Debt, opt => opt.MapFrom((src, dest) =>
                    (src.HasDebt && src.DebtAmount.HasValue && src.DebtAmount.Value > 0 && src.CustomerId.HasValue)
                        ? new Debt {
                            Date = src.Date,
                            Amount = src.DebtAmount.Value,
                            Notes = src.DebtNotes,
                            PersonId = src.CustomerId,
                            Direction = DebtDirection.Receivable,
                            SalesInvoice = dest
                        }
                        : null
                ));

            CreateMap<UpdateSalesInvoiceCommand, SalesInvoice>()
                .ForMember(d => d.ID, opt => opt.Ignore())
                .ForMember(d => d.Customer, opt => opt.Ignore())
                .ForMember(d => d.ReceiptVoucherSalesInvoices, opt => opt.Ignore())
                .ForMember(d => d.SalesInvoiceItems, opt => opt.Ignore());
        }
    }
}