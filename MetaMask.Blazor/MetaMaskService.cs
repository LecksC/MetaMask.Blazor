using MetaMask.Blazor.Enums;
using MetaMask.Blazor.Exceptions;
using MetaMask.Blazor.Extensions;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace MetaMask.Blazor
{
    // This class provides JavaScript functionality for MetaMask wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class MetaMaskService : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public static event Func<string, Task>? AccountChangedEvent;
        public static event Func<(int, Chain), Task>? NetworkChangedEvent;
        //public static event Func<Task>? ConnectEvent;
        //public static event Func<Task>? DisconnectEvent;

        public MetaMaskService(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => LoadScripts(jsRuntime).AsTask());
        }

        public ValueTask<IJSObjectReference> LoadScripts(IJSRuntime jsRuntime)
        {
            //await jsRuntime.InvokeAsync<IJSObjectReference>("import", "https://cdn.ethers.io/lib/ethers-5.1.0.umd.min.js");
            return jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/MetaMask.Blazor/metaMaskJsInterop.js");
        }

        public async ValueTask ConnectMetaMask()
        {
            var module = await moduleTask.Value;
            try
            {
                await module.InvokeVoidAsync("checkMetaMask");
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<bool> HasMetaMask()
        {
            var module = await moduleTask.Value;
            try
            {
                return await module.InvokeAsync<bool>("hasMetaMask");
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<bool> IsSiteConnected()
        {
            var module = await moduleTask.Value;
            try
            {
                return await module.InvokeAsync<bool>("isSiteConnected");
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask ListenToEvents()
        {
            var module = await moduleTask.Value;
            try
            {
                await module.InvokeVoidAsync("listenToChangeEvents");
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<string> GetSelectedAddress()
        {
            var module = await moduleTask.Value;
            try
            {
                return await module.InvokeAsync<string>("getSelectedAddress", null);
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<(int chainId, Chain chain)> GetSelectedChain()
        {
            var module = await moduleTask.Value;
            try
            {
                string chainHex = await module.InvokeAsync<string>("getSelectedChain", null);
                return ChainHexToChainResponse(chainHex);
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        private static (int chainId, Chain chain) ChainHexToChainResponse(string chainHex)
        {
            int chainId = chainHex.HexToInt();
            return (chainId, (Chain)chainId);
        }

        public async ValueTask<int> GetTransactionCount()
        {
            var module = await moduleTask.Value;
            try
            {
                var result = await module.InvokeAsync<JsonElement>("getTransactionCount");
                var resultString = result.GetString();
                if (resultString != null)
                {
                    int intValue = resultString.HexToInt();
                    return intValue;
                }
                return 0;
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<string> SignTypedData(string label, string value)
        {
            var module = await moduleTask.Value;
            try
            {
                return await module.InvokeAsync<string>("signTypedData", label, value);
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<string> SendTransaction(string to, BigInteger weiValue, string? data = null)
        {
            var module = await moduleTask.Value;
            try
            {
                string hexValue = "0x" + weiValue.ToString("X");
                return await module.InvokeAsync<string>("sendTransaction", to, hexValue, data);
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        public async ValueTask<dynamic> GenericRpc(string method, params dynamic?[]? args)
        {
            var module = await moduleTask.Value;
            try
            {
                return await module.InvokeAsync<dynamic>("genericRpc", method, args);
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
                throw;
            }
        }

        //[JSInvokable()]
        //public static async Task OnConnect()
        //{
        //    Console.WriteLine("connected");
        //    if (ConnectEvent != null)
        //    {
        //        await ConnectEvent.Invoke();
        //    }
        //}

        //[JSInvokable()]
        //public static async Task OnDisconnect()
        //{
        //    Console.WriteLine("disconnected");
        //    if (DisconnectEvent != null)
        //    {
        //        await DisconnectEvent.Invoke();
        //    }
        //}

        [JSInvokable()]
        public static async Task OnAccountsChanged(string selectedAccount)
        {
            if (AccountChangedEvent != null)
            {
                await AccountChangedEvent.Invoke(selectedAccount);
            }
        }

        [JSInvokable()]
        public static async Task OnNetworkChanged(string chainhex)
        {
            if (NetworkChangedEvent != null)
            {
                await NetworkChangedEvent.Invoke(ChainHexToChainResponse(chainhex));
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }

        private void HandleExceptions(Exception ex)
        {
            switch (ex.Message)
            {
                case "NoMetaMask":
                    throw new NoMetaMaskException();
                case "UserDenied":
                    throw new UserDeniedException();
            }
        }
    }
}
