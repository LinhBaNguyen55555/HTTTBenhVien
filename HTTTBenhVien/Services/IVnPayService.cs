using HTTTBenhVien.Models;
using Microsoft.AspNetCore.Http;

namespace HTTTBenhVien.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
