﻿using FluentFTP;
using FluentFTP.Proxy;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Requests.Ftp
{
    [BlockCategory("FTP", "Blocks to work with the FTP protocol", "#fbec5d")]
    public static class Methods
    {
        [Block("Connects to an FTP server")]
        public static async Task FtpConnect(BotData data, string host, int port = 21, 
            string username = "", string password = "", int timeoutMilliseconds = 10000)
        {
            data.Logger.LogHeader();

            if (data.Proxy is not null && data.Proxy.Type != Models.Proxies.ProxyType.Http)
            {
                throw new Exception("Currently, this block only supports HTTP proxies");
            }

            FtpClient client;

            if (data.Proxy is null)
            {
                client = new FtpClient(host, port, username, password)
                {
                    ConnectTimeout = timeoutMilliseconds,
                    DataConnectionConnectTimeout = timeoutMilliseconds,
                    DataConnectionReadTimeout = timeoutMilliseconds,
                    ReadTimeout = timeoutMilliseconds
                };
            }
            else
            {
                var proxyInfo = new ProxyInfo
                {
                    Host = data.Proxy.Host,
                    Port = data.Proxy.Port,
                    Credentials = new NetworkCredential(data.Proxy.Username, data.Proxy.Password)
                };

                client = new FtpClientHttp11Proxy(proxyInfo)
                {
                    Host = host,
                    Port = port,
                    Credentials = new NetworkCredential(username, password),
                    ConnectTimeout = timeoutMilliseconds,
                    DataConnectionConnectTimeout = timeoutMilliseconds,
                    DataConnectionReadTimeout = timeoutMilliseconds,
                    ReadTimeout = timeoutMilliseconds
                };
            }

            data.Objects["ftpClient"] = client;
            await client.AutoConnectAsync(data.CancellationToken);
            data.Logger.Log($"Connected to {host}:{port}", LogColors.Maize);
        }

        [Block("Lists the folders on the FTP server")]
        public static async Task<List<string>> FtpListItems(BotData data, FtpItemKind kind = FtpItemKind.FilesAndFolders, bool recursive = false)
        {
            data.Logger.LogHeader();
            var client = GetClient(data);
            var options = recursive ? FtpListOption.Recursive : FtpListOption.Auto;
            var list = new List<string>();

            foreach (var item in await client.GetListingAsync("/", options))
            {
                if (item.Type == FtpFileSystemObjectType.Directory && 
                    (kind == FtpItemKind.FilesAndFolders || kind == FtpItemKind.Folder))
                {
                    data.Logger.Log(item.FullName, LogColors.Maize);
                    list.Add(item.FullName);
                }

                if (item.Type == FtpFileSystemObjectType.File &&
                    (kind == FtpItemKind.FilesAndFolders || kind == FtpItemKind.File))
                {
                    data.Logger.Log(item.FullName, LogColors.Maize);
                    list.Add(item.FullName);
                }
            }

            return list;
        }

        [Block("Downloads a file from the FTP server")]
        public static async Task FtpDownloadFile(BotData data, string remoteFileName, string localFileName)
        {
            data.Logger.LogHeader();
            var client = GetClient(data);
            await client.DownloadFileAsync(localFileName, remoteFileName, FtpLocalExists.Overwrite, 
                FtpVerify.None, null, data.CancellationToken);

            data.Logger.Log($"{remoteFileName} downloaded to {localFileName}", LogColors.Maize);
        }

        [Block("Downloads a folder from the FTP server")]
        public static async Task FtpDownloadFolder(BotData data, string remoteDir, string localDir, 
            FtpLocalExists existsPolicy = FtpLocalExists.Skip)
        {
            data.Logger.LogHeader();
            var client = GetClient(data);
            await client.DownloadDirectoryAsync(localDir, remoteDir, FtpFolderSyncMode.Update, existsPolicy,
                FtpVerify.None, null, null, data.CancellationToken);

            data.Logger.Log($"{remoteDir} downloaded to {localDir}", LogColors.Maize);
        }

        [Block("Uploads a file to the FTP server")]
        public static async Task FtpUploadFile(BotData data, string remoteFileName, string localFileName,
            FtpRemoteExists existsPolicy = FtpRemoteExists.Overwrite)
        {
            data.Logger.LogHeader();
            var client = GetClient(data);
            await client.UploadFileAsync(localFileName, remoteFileName, existsPolicy, true, 
                FtpVerify.None, null, data.CancellationToken);

            data.Logger.Log($"{localFileName} uploaded to {remoteFileName}", LogColors.Maize);
        }

        [Block("Disconnects from the connected FTP server")]
        public static async Task FtpDisconnect(BotData data)
        {
            data.Logger.LogHeader();
            var client = GetClient(data);
            await client.DisconnectAsync(data.CancellationToken);

            data.Logger.Log("Disconnected from the FTP server", LogColors.Maize);
        }

        private static FtpClient GetClient(BotData data)
        {
            if (!data.Objects.ContainsKey("ftpClient"))
            {
                throw new Exception("Connect to a server first!");
            }

            return data.Objects["ftpClient"] as FtpClient;
        }
    }

    public enum FtpItemKind
    {
        Folder,
        File,
        FilesAndFolders
    }
}
