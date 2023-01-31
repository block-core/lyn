using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Newtonsoft.Json.Linq;
using Transaction = Lyn.Types.Bitcoin.Transaction;

namespace Lyn.Protocol.Bolt2.Wallet
{
    public class WalletTransactions : IWalletTransactions
    {
        private readonly ISerializationFactory _serializationFactory;
        private readonly ILogger<WalletTransactions> _logger;

        private NBitcoin.RPC.RPCClient? _client;

        public WalletTransactions(ISerializationFactory serializationFactory, ILogger<WalletTransactions> logger)
        {
            _serializationFactory = serializationFactory;
            _logger = logger;
        }

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

            _logger.LogDebug("Trx id of submitted funding trx - " + result.ResultString);
            
            if (result is null)
                throw new InvalidOperationException();
        }

        public async Task<ShortChannelId> LookupShortChannelIdByTransactionHashAsync(UInt256 hash,
            ushort outputIndex)
        {
            var client = GetClient();

            var result = await client.GetRawTransactionInfoAsync(new uint256(hash.GetBytes()));

            var blockDetails = await client.GetBlockAsync(result.BlockHash);

            var blockHeight = blockDetails.GetCoinbaseHeight().Value;
            var transactionIndex = blockDetails.Transactions.FindIndex(_ => _.GetHash().Equals(result.TransactionId));

            return new ShortChannelId(blockHeight,transactionIndex, outputIndex);
        }

        public async Task<long> GetMinimumFeeAsync()
        {
            var client = GetClient();

            var fee = await client.SendCommandWithNamedArgsAsync("estimatesmartfee",
                new Dictionary<string, object> { { "conf_target", 6 }, { "estimate_mode", "economical" } });

           return (long)fee.Result["feerate"].ToObject(typeof(long)); //TODO need to check this logic in signet
        }


        private NBitcoin.RPC.RPCClient GetClient()
        {
            if (_client is not null) 
                return _client;
            
            var uriBuilder = new UriBuilder(new Uri("http://127.0.0.1"));
            uriBuilder.Port = 18444;//38332;//
            
            _client = new NBitcoin.RPC.RPCClient(new NetworkCredential("bitcoin","bitcoin"), uriBuilder.Uri,Network.RegTest);

            return _client;
        }

        private string GetTransactionHex(Transaction transaction)
        {
            return Hex.ToString(_serializationFactory.Serialize(transaction));
        }
    }
}