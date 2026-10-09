using AutoMapper;
using PerfumeStore.Application.PurchaseInvoiceItems.Dtos;
using PerfumeStore.Application.PurchaseInvoices.Commands.CreatePurchaseInvoice;
using PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice;
using PerfumeStore.Application.SalesInvoiceItems.Dtos;
using PerfumeStore.Application.SalesInvoices.Commands.CreateSalesInvoice;
using PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice;
using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Application.PurchaseInvoices.Dtos {
    public class PurchaseInvoicesProfile : Profile {
        public PurchaseInvoicesProfile() {
            CreateMap<PurchaseInvoice, PurchaseInvoiceDto>()
                .ForMember(d => d.SupplierName, opt => opt.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null))
                .ForMember(d => d.DebtAmount, opt => opt.MapFrom(s => s.Debt != null ? s.Debt.Amount : 0))
                .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.PurchaseInvoiceItems != null ? s.PurchaseInvoiceItems.Count : 0));


            CreateMap<PurchaseInvoiceItem, PurchaseInvoiceItemDetailsDto>()
                .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : null));

            CreateMap<PurchaseInvoice, PurchaseInvoiceDetailsDto>()
                .ForMember(d => d.DebtNotes, opt => opt.MapFrom(s => s.Debt != null ? s.Debt.Notes : null))
                .ForMember(d => d.AppliedVoucherPayments, opt => opt.MapFrom(s => s.PaymentVoucherPurchaseInvoices.Sum(v => v.AppliedAmount)))
                .ForMember(d => d.SupplierName, opt => opt.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null))
                .ForMember(d => d.DebtAmount, opt => opt.MapFrom(s => s.Debt != null ? s.Debt.Amount : 0))
                .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.PurchaseInvoiceItems.Count))
                .ForMember(d => d.Products, opt => opt.MapFrom(s => s.PurchaseInvoiceItems))
                .ForMember(d => d.DebtAmount, opt => opt.MapFrom(s => s.Debt != null && !s.Debt.IsDeleted ? s.Debt.Amount : 0));

            CreateMap<CreatePurchaseInvoiceCommand, PurchaseInvoice>()
                .ForMember(d => d.ID, opt => opt.Ignore())
                .ForMember(d => d.Supplier, opt => opt.Ignore())
                .ForMember(d => d.PaymentVoucherPurchaseInvoices, opt => opt.Ignore())
                .ForMember(d => d.PurchaseInvoiceItems, opt => opt.Ignore())
                .ForMember(d => d.Debt, opt => opt.MapFrom((src, dest) =>
                    (src.HasDebt && src.DebtAmount.HasValue && src.DebtAmount.Value > 0)
                        ? new Debt {
                            Date = src.Date,
                            Amount = src.DebtAmount.Value,
                            Notes = src.DebtNotes,
                            PersonId = src.SupplierId,
                            Direction = DebtDirection.Payable,
                            PurchaseInvoice = dest
                        }
                        : null
                ));
            CreateMap<UpdatePurchaseInvoiceCommand, PurchaseInvoice>()
                .ForMember(d => d.ID, opt => opt.Ignore())
                .ForMember(d => d.Supplier, opt => opt.Ignore())
                .ForMember(d => d.PaymentVoucherPurchaseInvoices, opt => opt.Ignore())
                .ForMember(d => d.PurchaseInvoiceItems, opt => opt.Ignore());
        }
    }
}