﻿using SolcNet.DataDescription.Output;
using SolCodeGen.JsonRpc;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SolCodeGen
{

    public class SendParams
    {
        public Address? From { get; set; }
        public Address? To { get; set; }
        public UInt256? Value { get; set; }
    }

    public enum CallType
    {
        /// <summary>
        /// Creates new message call transaction on the block chain.
        /// </summary>
        Transaction,
        /// <summary>
        /// Executes a new message call immediately without creating a transaction on the block chain.
        /// </summary>
        Call
    }

    public abstract class BaseContract
    {
        public readonly Uri Server;
        public readonly Address ContractAddress;
        public readonly Address DefaultFromAccount;

        public abstract string AbiJson { get; }
        public abstract string DevDocJson { get; }
        public abstract string UserDocJson { get; }

        protected Abi _abi;
        public Abi Abi => _abi ?? (_abi = AbiJson);

        protected Doc _devDoc;
        public Doc DevDoc => _devDoc ?? (_devDoc = DevDocJson);

        protected Doc _userDoc;
        public Doc UserDoc => _userDoc ?? (_userDoc = UserDocJson);

        public JsonRpcClient JsonRpcClient { get; protected set; }

        public BaseContract(Uri server, Address contractAddress, Address defaultFromAccount)
        {
            Server = server;
            ContractAddress = contractAddress;
            DefaultFromAccount = defaultFromAccount;
            JsonRpcClient = new JsonRpcClient(Server, ContractAddress, DefaultFromAccount);
        }


    }

    public class ContractConstructParams
    {
        public Uri Server { get; set; }

        public Address DefaultFromAccount { get; set; }
    }
}