using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BayProxy
{
    using System.Web;
    using System.Web.UI;
    using System.Net;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Linq;

    /// <summary>
    /// www.devvblog.com
    /// </summary>
    public class BayProxy : Page
    {
        private readonly static String _THEPIRATEBAY = "thepiratebay.se";

        private readonly static Dictionary<String, String> ContentHeader = new Dictionary<String, String> { { "jpg", "image/jpg" }, { "png", "image/png" }, { "gif", "image/gif" }, { "css", "text/css" }, { "xml", "application/opensearchdescription+xml" }, { "js", "text/javascript" }, { "torrent", "application/x-bittorrent" } };

        private String _sFQDN;

        public BayProxy()
            : base()
        {
        }

        protected void Run()
        {
            Uri url = null;
            try
            {
                if (Request.QueryString.Count > 0 && Request.QueryString["p"] != null)
                {
                    Uri.TryCreate(Request.QueryString["p"], UriKind.RelativeOrAbsolute, out url);
                    if (url != null && Uri.IsWellFormedUriString(url.ToString(), UriKind.RelativeOrAbsolute))
                    {
                        if (!this.IsFile(url))
                        {
                            this._sFQDN = this.GetFQDN(url);
                            Session["FQDN"] = this._sFQDN;
                            Response.Write(this.GetParsedHTML(this.GetData(url)));
                        }
                        else
                        {
                            this.GetFile(url);
                        }
                    }
                }
                else
                {
                    if (Request.QueryString.Count > 0 && Request.QueryString["q"] != null && Session["FQDN"] != null)
                    {
                        this._sFQDN = Session["FQDN"].ToString();
                        Uri.TryCreate(String.Format("{0}/search/{1}", this._sFQDN, Request.QueryString["q"]), UriKind.RelativeOrAbsolute, out url);
                        if (url != null && Uri.IsWellFormedUriString(url.ToString(), UriKind.RelativeOrAbsolute))
                        {
                            Response.Write(this.GetParsedHTML(this.GetData(url)));
                        }
                    }
                    else
                    {
                        Response.Redirect(String.Format("{0}?p={1}", Path.GetFileName(Request.PhysicalPath), _THEPIRATEBAY), true);
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message);
            }
        }

        private String GetScheme(Uri url)
        {
            if (!new Regex(@"(http://|https://)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).IsMatch(url.ToString()))
            {
                return "http://";
            }
            return "";
        }

        private void GetFile(Uri url)
        {
            WebClient webClient = null;
            Match regexMatch = null;
            String sKey = "";
            try
            {
                regexMatch = new Regex(@"\.(?<Extension>" + this.GetExtensions() + ")$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Match(url.ToString());
                if (regexMatch.Success && regexMatch.Groups["Extension"] != null)
                {
                    sKey = regexMatch.Groups["Extension"].Value.ToLowerInvariant();
                    if (ContentHeader.ContainsKey(sKey))
                    {
                        webClient = new WebClient();
                        Response.ContentType = ContentHeader[sKey];
                        var test = this.GetScheme(url) + url;
                        Response.BinaryWrite(webClient.DownloadData(this.GetScheme(url) + url));
                    }
                }
            }
            finally
            {
                webClient.Dispose();
            }
        }

        private bool IsHost(String sHost)
        {
            if (!String.IsNullOrEmpty(sHost))
            {
                return sHost.Split('.').Count() > 1 && !new Regex(@"php|html|htm|asp|aspx", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).IsMatch(sHost);
            }
            return false;
        }

        private String GetParsedHTMLDelegate(Match regexMatch)
        {
            String sUrl = "";
            String sHost = "";
            Regex regex = new Regex(@"^//$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (regexMatch.Success && regexMatch.Groups["Attribute"] != null && regexMatch.Groups["URL"] != null)
            {
                sUrl = regexMatch.Groups["URL"].Value;
                do
                {
                    sUrl = sUrl.Trim('/');
                }
                while (regex.IsMatch(sUrl));
                sHost = this.GetHost(sUrl);
                if (this.IsHost(sHost))
                {
                    if (new Regex(this._sFQDN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).IsMatch(sHost))
                    {
                        return String.Format("{0}=\"{1}?p={2}\"", regexMatch.Groups["Attribute"].Value, Path.GetFileName(Request.PhysicalPath), sUrl);
                    }
                    else
                    {
                        return String.Format("{0}=\"{1}\"", regexMatch.Groups["Attribute"].Value, sUrl);
                    }
                }
                else
                {
                    return String.Format("{0}=\"{1}?p={2}\"", regexMatch.Groups["Attribute"].Value, Path.GetFileName(Request.PhysicalPath), this._sFQDN + "/" + sUrl);
                }
            }
            return regexMatch.Success ? regexMatch.ToString() : "";
        }

        private String GetParsedHTML(String sData)
        {
            if (!String.IsNullOrEmpty(sData))
            {
                return new Regex(@"(?<Attribute>href|src|action)=(""|\')(?<URL>(http://|https://)?([a-z0-9\.\-]+)?(:[0-9]+)?[a-z0-9\._/\-\?=%&()[\]\+!]+)(""|\')", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline).Replace(sData, this.GetParsedHTMLDelegate);
            }
            return "";
        }

        private String GetData(Uri url)
        {
            WebClient webClient = null;
            try
            {
                webClient = new WebClient();
                webClient.Headers.Add("User-Agent", "Opera/9.23 (Windows NT 5.1; U; en)");
                return webClient.DownloadString(this.GetScheme(url) + url);
            }
            finally
            {
                webClient.Dispose();
            }
        }

        private String GetFQDN(Uri url)
        {
            Match regexMatch = new Regex(@"^(http://|https://)?(?<FQDN>[a-z0-9\._\-]+(:[0-9]+)?)/?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Match(url.ToString());
            if (regexMatch.Success && regexMatch.Groups["FQDN"] != null)
            {
                return regexMatch.Groups["FQDN"].Value;
            }
            throw new NotSupportedException("Whoops, something went wrong. Please try again.");
        }

        private String GetHost(String sUrl)
        {
            String sHost = "";
            Match regexMatch = new Regex(@"^(http:/|https:/)?(/+)?(?<Host>[a-z0-9\.\-]+)?(:[0-9]+)?/?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Match(sUrl);
            if (regexMatch.Success && regexMatch.Groups["Host"] != null)
            {
                sHost = regexMatch.Groups["Host"].Value;
                if (sHost.Split('.').Count() > 2)
                {
                    regexMatch = new Regex(@"([a-z0-9-]+)\.([a-z]{2,}|\.[a-z]{2,}\.[a-z]{2,})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Match(sHost);
                    if (regexMatch.Success)
                    {
                        sHost = regexMatch.ToString();
                    }
                }
            }
            return sHost;
        }

        private String GetExtensions()
        {
            return String.Join("|", ContentHeader.Select(x => x.Key).ToArray()).Trim('|');
        }

        private bool IsFile(Uri url)
        {
            return new Regex(@"^(http://|https://)?[a-z0-9\.\-]+(:[0-9]+)?[a-z0-9\._/\-\?=%&()[\]\+!]+\.(" + this.GetExtensions() + ")$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).IsMatch(url.ToString());
        }
    }
}
