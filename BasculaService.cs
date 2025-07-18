using System;
using System.Threading.Tasks;

namespace costbenefi.Services
{
    public class PesoRecibidoEventArgs : EventArgs
    {
        public decimal Peso { get; set; }
    }

    public class BasculaService : IDisposable
    {
        public event EventHandler<PesoRecibidoEventArgs> PesoRecibido;
        public event EventHandler<string> ErrorOcurrido;

        private bool _conectada = false;
        private decimal _pesoActual = 0;
        private Random _random = new Random();

        public bool Conectar()
        {
            try
            {
                _conectada = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, ex.Message);
                return false;
            }
        }

        public async Task<decimal> LeerPesoAsync()
        {
            if (!_conectada)
                throw new InvalidOperationException("Báscula no conectada");

            await Task.Delay(100);

            // Generar peso simulado
            _pesoActual = (decimal)(_random.NextDouble() * 5.0);
            _pesoActual = Math.Round(_pesoActual, 3);

            PesoRecibido?.Invoke(this, new PesoRecibidoEventArgs { Peso = _pesoActual });
            return _pesoActual;
        }

        public void Tarar()
        {
            _pesoActual = 0;
            PesoRecibido?.Invoke(this, new PesoRecibidoEventArgs { Peso = 0 });
        }

        public void Dispose()
        {
            _conectada = false;
        }
    }
}