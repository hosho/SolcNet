﻿using Newtonsoft.Json;
using SolcNet.CompileErrors;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using SolcNet.NativeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SolcNet
{
    public class SolcLib
    {
        INativeSolcLib _native;

        public string VersionDescription => _native.GetVersion();
        public Version Version => Version.Parse(VersionDescription.Split(new [] { "-" }, 2, StringSplitOptions.RemoveEmptyEntries)[0]);

        public string License => _native.GetLicense();

        string _solSourceRoot = null;
        string _lastSourceDir = null;

        public SolcLib(string solSourceRoot = null)
        {
            _native = new SolcLibDynamicProvider();
            _solSourceRoot = solSourceRoot;
        }

        public SolcLib(INativeSolcLib nativeLib, string solSourceRoot = null)
        {
            _native = nativeLib;
            _solSourceRoot = solSourceRoot;
        }

        public static SolcLib Create(string solSourceRoot = null)
        {
            return new SolcLib(solSourceRoot);
        }

        public static SolcLib Create<TNativeLib>(string solSourceRoot = null) where TNativeLib : INativeSolcLib, new()
        {
            return new SolcLib(new TNativeLib(), solSourceRoot);
        }

        private OutputDescription CompileInputDescriptionJson(string jsonInput, 
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError)
        {
            var res = _native.Compile(jsonInput, ReadSolSourceFileManaged);
            var output = OutputDescription.FromJsonString(res);

            var compilerException = CompilerException.GetCompilerExceptions(output.Errors, errorHandling);
            if (compilerException != null)
            {
                throw compilerException;
            }
            return output;
        }

        public OutputDescription Compile(InputDescription input, 
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError)
        {
            var jsonStr = input.ToJsonString();
            return CompileInputDescriptionJson(jsonStr, errorHandling);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(string contractFilePaths,
            OutputType[] outputSelection,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError)
        {
            return Compile(new[] { contractFilePaths }, outputSelection ?? OutputTypes.All, errorHandling);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(string contractFilePaths,
            OutputType? outputSelection = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError)
        {
            return Compile(new[] { contractFilePaths }, outputSelection, errorHandling);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(string[] contractFilePaths,
            OutputType? outputSelection = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError)
        {
            var outputs = outputSelection == null ? OutputTypes.All : OutputTypes.GetItems(outputSelection.Value);
            return Compile(contractFilePaths, outputs, errorHandling);
        }

        public OutputDescription Compile(string[] contractFilePaths,
            OutputType[] outputSelection, 
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError)
        {
            var inputDesc = new InputDescription();
            inputDesc.Settings.OutputSelection["*"] = new Dictionary<string, OutputType[]>
            {
                ["*"] = outputSelection.ToArray()
            };

            foreach (var filePath in contractFilePaths)
            {
                var source = new Source { Urls = new List<string> { filePath } };
                inputDesc.Sources.Add(filePath, source);
            }

            return Compile(inputDesc, errorHandling);
        }

        void ReadSolSourceFileManaged(string path, ref string contents, ref string error)
        {
            try
            {
                string sourceFilePath = path;
                // if given path is relative and a root is provided, combine them
                if (!Path.IsPathRooted(path) && _solSourceRoot != null)
                {
                    sourceFilePath = Path.Combine(_solSourceRoot, path);
                }
                if (!File.Exists(sourceFilePath) && _lastSourceDir != null)
                {
                    sourceFilePath = Path.Combine(_lastSourceDir, path);
                }
                if (File.Exists(sourceFilePath))
                {
                    _lastSourceDir = Path.GetDirectoryName(sourceFilePath);
                    contents = File.ReadAllText(sourceFilePath, Encoding.UTF8);
                }
                else
                {
                    error = "Source file not found: " + path;
                }
            }
            catch (Exception ex)
            {
                error = ex.ToString();
            }
        }

    }
}
