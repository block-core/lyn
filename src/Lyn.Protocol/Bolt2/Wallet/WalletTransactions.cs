using System;
using System.Net;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using NBitcoin;
using Newtonsoft.Json.Linq;
using Transaction = Lyn.Types.Bitcoin.Transaction;

namespace Lyn.Protocol.Bolt2.Wallet
{
    public class WalletTransactions : IWalletTransactions
    {
        private readonly ISerializationFactory _serializationFactory;

        private NBitcoin.RPC.RPCClient? _client;

        public WalletTransactions(ISerializationFactory serializationFactory)
        {
            _serializationFactory = serializationFactory;
        }
//bcrt1qhn9gawy9vv73tlkaunaunwzea9ula0qj8pe9yn
        public async Task<bool> IsAmountAvailableAsync(Satoshis amount)
        {
            var client = GetClient();

            var response = await client.GetBalanceAsync();

            return response.Satoshi.CompareTo((long)amount) > 0;
        }

        public async Task<Transaction> GenerateTransactionForOutputAsync(TransactionOutput transactionOutput)
        {
            var client = GetClient();

            var hex = GetTransactionHex(new Transaction { Outputs = new[] { transactionOutput }, Version = 2 });

            var response = await client.SendCommandAsync("fundrawtransaction", hex);
            
            var r = (JObject)response.Result;
            
            var signedTransactionResult = await client.SendCommandAsync("signrawtransactionwithwallet", r["hex"].Value<string>());
            
            var raw = (JObject)signedTransactionResult.Result;
            
            return _serializationFactory.Deserialize<Transaction>(Hex.FromString(raw["hex"].Value<string>()));
        }

        public async Task PublishTransactionAsync(Transaction transaction)
        {
            var client = GetClient();
            
            var result = await client.SendCommandAsync("sendrawtransaction", GetTransactionHex(transaction));

            if (result is null)
                throw new InvalidOperationException();
        }


        private NBitcoin.RPC.RPCClient GetClient()
        {
            if (_client is not null) 
                return _client;
            
            var uriBuilder = new UriBuilder(new Uri("http://127.0.0.1"));
            uriBuilder.Port = 18443;
            
            _client = new NBitcoin.RPC.RPCClient(new NetworkCredential("regtest","regtest"), uriBuilder.Uri,Network.RegTest);

            return _client;
        }

        private string GetTransactionHex(Transaction transaction)
        {
            return Hex.ToString(_serializationFactory.Serialize(transaction));
        }
    }
}