using System;

namespace costbenefi.Services
{
    public class ErrorPOSEventArgs : EventArgs
    {
        public string TipoDispositivo { get; set; }
        public string Mensaje { get; set; }
    }

    public class POSIntegrationService : IDisposable
    {
        private readonly TicketPrinter _ticketPrinter;
        private readonly BasculaService _basculaService;
        private readonly ScannerPOSService _scannerService;

        public event EventHandler<ErrorPOSEventArgs> ErrorOcurrido;

        public POSIntegrationService(TicketPrinter ticketPrinter, BasculaService basculaService, ScannerPOSService scannerService)
        {
            _ticketPrinter = ticketPrinter;
            _basculaService = basculaService;
            _scannerService = scannerService;

            // Configurar eventos
            _basculaService.ErrorOcurrido += (s, e) =>
                ErrorOcurrido?.Invoke(this, new ErrorPOSEventArgs { TipoDispositivo = "Bascula", Mensaje = e });

            _scannerService.ErrorEscaneo += (s, e) =>
                ErrorOcurrido?.Invoke(this, new ErrorPOSEventArgs { TipoDispositivo = "Scanner", Mensaje = e });
        }

        public void Dispose()
        {
            _ticketPrinter?.Dispose();
            _basculaService?.Dispose();
            _scannerService?.Dispose();
        }
    }
}