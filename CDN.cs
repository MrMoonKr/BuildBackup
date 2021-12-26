﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;


namespace BuildBackup
{
    /// <summary>
    /// CDN 웹 접근 및 다운로드 
    /// </summary>
    public class CDN
    {
        public HttpClient       client;
        public string           cacheDir;
        public List<string>     cdnList;

        public async Task<uint> GetRemoteFileSize( string path )
        {
            path = path.ToLower();
            var found = false;

            foreach ( var cdn in cdnList )
            {
                if ( found ) continue;

                var uri         = new Uri( "http://" + cdn + "/" + path );
                var cleanName   = uri.AbsolutePath;

                try
                {
                    using ( var response = await client.GetAsync( uri, HttpCompletionOption.ResponseHeadersRead ) )
                    {
                        if ( response.IsSuccessStatusCode ) // OK
                        {
                            found = true;

                            if ( response.Content.Headers.ContentLength != null )
                                return ( uint )response.Content.Headers.ContentLength ;
                        }
                        else if ( response.StatusCode == System.Net.HttpStatusCode.NotFound ) // Not Found
                        {
                            Logger.WriteLine( "File not found on CDN " + cdn + " trying next CDN (if available)..");
                        }
                        else // 에러
                        {
                            throw new FileNotFoundException( "Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + uri.AbsoluteUri );
                        }
                    }
                }
                catch ( Exception e )
                {
                    Logger.WriteLine("!!! Error retrieving file size " + uri.AbsoluteUri + ": " + e.Message);
                }
            }

            if ( !found )
            {
                Logger.WriteLine( "Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve filesize!", true);
            }

            return 0;
        }

        /// <summary>
        /// 웹으로 부터 비동기 다운로드
        /// </summary>
        /// <param name="path">루트이하 경로</param>
        /// <param name="returnstream"></param>
        /// <param name="redownload"></param>
        /// <param name="expectedSize"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public async Task<byte[]> Get( string path, bool returnstream = true, bool redownload = false, uint expectedSize = 0, bool verbose = false )
        {
            path            = path.ToLower();
            var localPath   = Path.Combine( cacheDir, path );

            if ( File.Exists( localPath ) && expectedSize != 0 ) // 다운로드한 로컬 파일 체크
            {
                var fileInfo = new FileInfo( localPath );
                if ( fileInfo.Length != expectedSize )
                {
                    if ( verbose )
                        Console.WriteLine("File size of " + path + " " + fileInfo.Length + " does not match expected size " + expectedSize + " redownloading..");
                    
                    redownload = true;
                }
            }

            if ( redownload || !File.Exists( Path.Combine( cacheDir, path ) ) ) // 새로 다운로드 또는 처음 다운로드
            {
                var found = false;

                foreach ( var cdn in cdnList )
                {
                    if ( found ) continue;

                    var uri         = new Uri( "http://" + cdn + "/" + path );
                    var cleanName   = uri.AbsolutePath;

                    if ( verbose )
                    {
                        Console.WriteLine( "Downloading " + path );
                    }

                    try
                    {
                        if ( !Directory.Exists( cacheDir + cleanName ) ) // 저장 폴더 체크 및 생성
                        {
                            Directory.CreateDirectory( Path.GetDirectoryName( cacheDir + cleanName ) );
                        }

                        using ( var response        = await client.GetAsync( uri, HttpCompletionOption.ResponseHeadersRead ) )
                        using ( var responseStream  = await response.Content.ReadAsStreamAsync() )
                        using ( var file            = File.Create( cacheDir + cleanName ) )
                        {
                            if ( response.IsSuccessStatusCode )
                            {
                                found = true;

                                var buffer          = new byte[4096];
                                int read;
                                while ( ( read = await responseStream.ReadAsync( buffer, 0, buffer.Length ) ) != 0 )
                                {
                                    file.Write( buffer, 0, read );
                                }
                            }
                            else if ( response.StatusCode == System.Net.HttpStatusCode.NotFound )
                            {
                                Logger.WriteLine("File not found on CDN " + cdn + " trying next CDN (if available)..");
                            }
                            else
                            {
                                throw new FileNotFoundException("Error retrieving file: HTTP status code " +
                                                                response.StatusCode + " on URL " + uri.AbsoluteUri);
                            }
                        }
                    }
                    catch ( TaskCanceledException e )
                    {
                        if (!e.CancellationToken.IsCancellationRequested)
                        {
                            Logger.WriteLine("!!! Timeout while retrieving file " + uri.AbsoluteUri);
                        }
                    }
                    catch ( Exception e )
                    {
                        Logger.WriteLine("!!! Error retrieving file " + uri.AbsoluteUri + ": " + e.Message);
                    }
                }

                if (!found)
                {
                    Logger.WriteLine("Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve it!", true);
                }
                else
                {
                    if (verbose)
                        Console.WriteLine("Downloaded " + path);
                }
            }

            if (returnstream)
            {
                return await File.ReadAllBytesAsync(Path.Combine(cacheDir, path));
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
