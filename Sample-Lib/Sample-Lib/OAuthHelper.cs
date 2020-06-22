using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Data;
using Intuit.Ipp.Exception;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using Intuit.Ipp.Core.Configuration;

namespace SampleLib
{
    public class OAuthHelper
    {
        private OAuth2Client oauth2Client = null;
        private Dictionary<string, string> oauthTokens = new Dictionary<string, string>();
        private readonly bool IsDebugging = true;

        public OAuthHelper(string clientID, string clientSecret, string redirectURL, string environment)
        {
            // Instantiate object
            oauth2Client = new OAuth2Client(clientID, clientSecret, redirectURL, environment); // environment is "sandbox" or "production"
            oauth2Client.CSRFToken = oauth2Client.GenerateCSRFToken();
        }

        public string GetOAuth2URL()
        {
            List<OidcScopes> scopes = new List<OidcScopes>
            {
                OidcScopes.Accounting
            };

            //Get the authorization URL
            return oauth2Client.GetAuthorizationURL(scopes, oauth2Client.CSRFToken);
        }

        public Dictionary<string, string> GetTokens(NameValueCollection QueryString)
        {
            try
            {
                if (QueryString.Count > 0)
                {
                    AuthorizeResponse response = new AuthorizeResponse(QueryString.ToString());
                    if (response.State != null)
                    {
                        if (response.RealmId != null)
                        {
                            if (!oauthTokens.ContainsKey("realmId"))
                                oauthTokens.Add("realmId", response.RealmId);
                        }

                        if (response.Code != null)
                        {
                            System.Threading.Tasks.Task task = GetAuthTokensAsync(response.Code);
                            task.Wait();
                            Output("GetAuthTokensAsync Finished");
                        }
                    }
                    else
                        Output("response.State = null");
                }
            }
            catch (Exception ex)
            {
                Output(ex.Message);
            }
            return oauthTokens;
        }

        public Dictionary<string, string> RefreshTokens(string refreshToken)
        {
            try
            {
                //Refresh token endpoint
                var tokenResp = oauth2Client.RefreshTokenAsync(refreshToken);
                tokenResp.Wait();

                if (tokenResp.Result != null)
                {
                    if (!string.IsNullOrEmpty(tokenResp.Result.AccessToken) && !string.IsNullOrEmpty(tokenResp.Result.RefreshToken))
                    {
                        if (!oauthTokens.ContainsKey("accessToken"))
                            oauthTokens.Add("accessToken", tokenResp.Result.AccessToken);
                        else
                            oauthTokens["accessToken"] = tokenResp.Result.AccessToken;

                        if (!oauthTokens.ContainsKey("refreshToken"))
                            oauthTokens.Add("refreshToken", tokenResp.Result.RefreshToken);
                        else
                            oauthTokens["refreshToken"] = tokenResp.Result.RefreshToken;
                    }
                    else
                        Output("AccessToken or RefreshToken cannot be empty");
                }
                else
                    Output("Error Refreshing Tokens: " + tokenResp.Result.Raw);
            }
            catch (Exception ex)
            {
                Output(ex.Message);
            }
            return oauthTokens;
        }

        public bool RevokeTokens(string accessToken, string refreshToken)
        {
            try
            {
                Output("Performing Revoke tokens.");
                if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
                {
                    var revokeTokenResp = oauth2Client.RevokeTokenAsync(refreshToken);
                    revokeTokenResp.Wait();

                    if (revokeTokenResp.Result != null && revokeTokenResp.Result.HttpStatusCode == HttpStatusCode.OK)
                    {
                        oauthTokens.Clear();
                        Output("Token revoked.");
                        return true;
                    }
                    else
                    {
                        Output("Token Not Revoked.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Output(ex.Message);
            }
            return false;
        }

        public string MakeQBOQuery(string accessToken, string realmID, string refreshToken, int queryType, string query, bool IsProd)
        {
            try
            {
                if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(realmID))
                {
                    ServiceContext serviceContext = BuildServiceContext(accessToken, realmID, IsProd);
                    string output = "";
                    QBObjectType QBQueryType = (QBObjectType)queryType;

                    Output("Making MakeQBOQuery " + QBQueryType + " API Call.");

                    switch (QBQueryType)
                    {
                        case QBObjectType.cClass:
                            QueryService<Class> classQuerySvc = new QueryService<Class>(serviceContext);
                            Class classQuery = classQuerySvc.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer classSerializer = new XmlSerializer(typeof(Class));
                            StringWriter classWriter = new StringWriter();
                            classSerializer.Serialize(classWriter, classQuery);

                            output = classWriter.ToString();
                            break;
                        case QBObjectType.Customer:
                            QueryService<Customer> customerQuery = new QueryService<Customer>(serviceContext);
                            Customer customer = customerQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer customerSerializer = new XmlSerializer(typeof(Customer));
                            StringWriter customerWriter = new StringWriter();
                            customerSerializer.Serialize(customerWriter, customer);

                            output = customerWriter.ToString();
                            break;
                        case QBObjectType.Invoice:
                            QueryService<Invoice> invoiceQuery = new QueryService<Invoice>(serviceContext);
                            Invoice invoice = invoiceQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer invoiceSerializer = new XmlSerializer(typeof(Invoice));
                            StringWriter invoiceWriter = new StringWriter();
                            invoiceSerializer.Serialize(invoiceWriter, invoice);

                            output = invoiceWriter.ToString();
                            break;
                        case QBObjectType.InvoiceCreditMemo:
                            QueryService<CreditMemo> creditMemoQuery = new QueryService<CreditMemo>(serviceContext);
                            CreditMemo creditMemo = creditMemoQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer creditMemoSerializer = new XmlSerializer(typeof(CreditMemo));
                            StringWriter creditMemoWriter = new StringWriter();
                            creditMemoSerializer.Serialize(creditMemoWriter, creditMemo);

                            output = creditMemoWriter.ToString();
                            break;
                        case QBObjectType.Vendor:
                        case QBObjectType.VendorDeduction:
                            QueryService<Vendor> vendorQuery = new QueryService<Vendor>(serviceContext);
                            Vendor vendor = vendorQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer vendorSerializer = new XmlSerializer(typeof(Vendor));
                            StringWriter vendorWriter = new StringWriter();
                            vendorSerializer.Serialize(vendorWriter, vendor);

                            output = vendorWriter.ToString();
                            break;
                        case QBObjectType.CarrierPay:
                        case QBObjectType.DriverPay:
                            QueryService<Bill> billQuery = new QueryService<Bill>(serviceContext);
                            Bill bill = billQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer billSerializer = new XmlSerializer(typeof(Bill));
                            StringWriter billWriter = new StringWriter();
                            billSerializer.Serialize(billWriter, bill);

                            output = billWriter.ToString();
                            break;
                        case QBObjectType.DriverDeduction:
                            QueryService<VendorCredit> vendorCreditQuery = new QueryService<VendorCredit>(serviceContext);
                            VendorCredit vendorCredit = vendorCreditQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer vendorCreditSerializer = new XmlSerializer(typeof(VendorCredit));
                            StringWriter vendorCreditWriter = new StringWriter();
                            vendorCreditSerializer.Serialize(vendorCreditWriter, vendorCredit);

                            output = vendorCreditWriter.ToString();
                            break;
                        case QBObjectType.Term:
                            QueryService<Term> termQuery = new QueryService<Term>(serviceContext);
                            Term term = termQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer termSerializer = new XmlSerializer(typeof(Term));
                            StringWriter termWriter = new StringWriter();
                            termSerializer.Serialize(termWriter, term);

                            output = termWriter.ToString();
                            break;
                        case QBObjectType.COA:
                            QueryService<Account> accountQuery = new QueryService<Account>(serviceContext);
                            Account account = accountQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer accountSerializer = new XmlSerializer(typeof(Account));
                            StringWriter accountWriter = new StringWriter();
                            accountSerializer.Serialize(accountWriter, account);

                            output = accountWriter.ToString();
                            break;
                        case QBObjectType.TaxCode:
                            QueryService<TaxCode> taxCodeQuery = new QueryService<TaxCode>(serviceContext);
                            TaxCode taxCode = taxCodeQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer taxCodeSerializer = new XmlSerializer(typeof(TaxCode));
                            StringWriter taxCodeWriter = new StringWriter();
                            taxCodeSerializer.Serialize(taxCodeWriter, taxCode);

                            output = taxCodeWriter.ToString();
                            break;
                        case QBObjectType.TaxRate:
                            QueryService<TaxRate> taxRateQuery = new QueryService<TaxRate>(serviceContext);
                            TaxRate taxRate = taxRateQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer taxRateSerializer = new XmlSerializer(typeof(TaxRate));
                            StringWriter taxRateWriter = new StringWriter();
                            taxRateSerializer.Serialize(taxRateWriter, taxRate);

                            output = taxRateWriter.ToString();
                            break;
                        case QBObjectType.Item:
                            QueryService<Item> itemQuery = new QueryService<Item>(serviceContext);
                            Item item = itemQuery.ExecuteIdsQuery(query).FirstOrDefault();

                            XmlSerializer itemSerializer = new XmlSerializer(typeof(Item));
                            StringWriter itemWriter = new StringWriter();
                            itemSerializer.Serialize(itemWriter, item);

                            output = itemWriter.ToString();
                            break;
                    }

                    Output("MakeQBOQuery call successful.");
                    return output;
                }
                else
                    Output("AccessToken or RealmID cannot be empty");
            }
            catch (IdsException ex)
            {
                if (ex.Message == "Unauthorized-401")
                {
                    Output("Invalid/Expired Access Token.");

                    RefreshTokens(refreshToken);

                    if (oauthTokens.ContainsKey("accessToken") && oauthTokens.ContainsKey("refreshToken"))
                    {
                        Output("Refreshed Access Token");
                        return MakeQBOQuery(oauthTokens["accessToken"], realmID, oauthTokens["refreshToken"], queryType, query, IsProd);
                    }
                    else
                    {
                        Output("Error while refreshing tokens");
                        return "REVOKE";
                    }
                }
                else if (ex.Message == "429")
                {
                    Output("Too Many Concurrent Requests - Retrying in 1 minute...");
                    System.Threading.Thread.Sleep(60000);
                    return MakeQBOQuery(accessToken, realmID, refreshToken, queryType, query, IsProd);
                }
                else
                    Output("IdsException = " + ex.Message);
            }
            catch (Exception ex)
            {
                Output("Exception = " + ex.Message);
                Output("StackTrace = " + ex.StackTrace);
                Output("HelpLink = " + ex.HelpLink);
                Output("InnerException.Message = " + ex.InnerException.Message);
            }
            return "";
        }

        public string CreateEntity(string accessToken, string realmID, string refreshToken, int entityType, string XML, bool IsProd)
        {
            try
            {
                if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(realmID))
                {
                    string output = "";
                    ServiceContext serviceContext = BuildServiceContext(accessToken, realmID, IsProd);
                    DataService dataService = new DataService(serviceContext);
                    QBObjectType QBEntityType = (QBObjectType)entityType;

                    Output("Making CreateEntity " + QBEntityType + " API Call.");

                    switch (QBEntityType)
                    {
                        case QBObjectType.Customer:
                            XmlSerializer customerSerializer = new XmlSerializer(typeof(Customer));
                            StringReader customerReader = new StringReader(XML);
                            Customer customer = (Customer)customerSerializer.Deserialize(customerReader);
                            Customer newCustomer = dataService.Add(customer);

                            StringWriter customerWriter = new StringWriter();
                            customerSerializer.Serialize(customerWriter, newCustomer);

                            output = customerWriter.ToString();
                            break;
                        case QBObjectType.Invoice:
                            XmlSerializer invoiceSerializer = new XmlSerializer(typeof(Invoice));
                            StringReader invoiceReader = new StringReader(XML);
                            Invoice invoice = (Invoice)invoiceSerializer.Deserialize(invoiceReader);
                            Invoice newInvoice = dataService.Add(invoice);

                            StringWriter invoiceWriter = new StringWriter();
                            invoiceSerializer.Serialize(invoiceWriter, newInvoice);

                            output = invoiceWriter.ToString();
                            break;
                        case QBObjectType.InvoiceCreditMemo:
                            XmlSerializer creditMemoSerializer = new XmlSerializer(typeof(CreditMemo));
                            StringReader creditMemoReader = new StringReader(XML);
                            CreditMemo creditMemo = (CreditMemo)creditMemoSerializer.Deserialize(creditMemoReader);
                            CreditMemo newCreditMemo = dataService.Add(creditMemo);

                            StringWriter creditMemoWriter = new StringWriter();
                            creditMemoSerializer.Serialize(creditMemoWriter, newCreditMemo);

                            output = creditMemoWriter.ToString();
                            break;
                        case QBObjectType.Vendor:
                        case QBObjectType.VendorDeduction:
                            XmlSerializer vendorSerializer = new XmlSerializer(typeof(Vendor));
                            StringReader reader = new StringReader(XML);
                            Vendor vendor = (Vendor)vendorSerializer.Deserialize(reader);
                            Vendor newVendor = dataService.Add(vendor);

                            StringWriter vendorWriter = new StringWriter();
                            vendorSerializer.Serialize(vendorWriter, newVendor);

                            output = vendorWriter.ToString();
                            break;
                        case QBObjectType.CarrierPay:
                        case QBObjectType.DriverPay:
                            XmlSerializer billSerializer = new XmlSerializer(typeof(Bill));
                            StringReader billReader = new StringReader(XML);
                            Bill bill = (Bill)billSerializer.Deserialize(billReader);
                            Bill newBill = dataService.Add(bill);

                            StringWriter billWriter = new StringWriter();
                            billSerializer.Serialize(billWriter, newBill);

                            output = billWriter.ToString();
                            break;
                        case QBObjectType.DriverDeduction:
                            XmlSerializer vendorCreditSerializer = new XmlSerializer(typeof(VendorCredit));
                            StringReader vendorCreditReader = new StringReader(XML);
                            VendorCredit vendorCredit = (VendorCredit)vendorCreditSerializer.Deserialize(vendorCreditReader);
                            VendorCredit newVendorCredit = dataService.Add(vendorCredit);

                            StringWriter vendorCreditWriter = new StringWriter();
                            vendorCreditSerializer.Serialize(vendorCreditWriter, newVendorCredit);

                            output = vendorCreditWriter.ToString();
                            break;
                        case QBObjectType.TaxCode:
                            XmlSerializer taxCodeSerializer = new XmlSerializer(typeof(TaxCode));
                            StringReader taxCodeReader = new StringReader(XML);
                            TaxCode taxCode = (TaxCode)taxCodeSerializer.Deserialize(taxCodeReader);
                            TaxCode newTaxCode = dataService.Add(taxCode);

                            StringWriter taxCodeWriter = new StringWriter();
                            taxCodeSerializer.Serialize(taxCodeWriter, newTaxCode);

                            output = taxCodeWriter.ToString();
                            break;
                        case QBObjectType.TaxRate:
                            XmlSerializer taxRateSerializer = new XmlSerializer(typeof(TaxRate));
                            StringReader taxRateReader = new StringReader(XML);
                            TaxRate taxRate = (TaxRate)taxRateSerializer.Deserialize(taxRateReader);
                            TaxRate newTaxRate = dataService.Add(taxRate);

                            StringWriter taxRateWriter = new StringWriter();
                            taxRateSerializer.Serialize(taxRateWriter, newTaxRate);

                            output = taxRateWriter.ToString();
                            break;
                    }
                    Output("QBO CreateEntity successful.");
                    return output;
                }
                else
                    Output("AccessToken or RealmID cannot be empty");
            }
            catch (IdsException ex)
            {
                if (ex.Message == "Unauthorized-401")
                {
                    Output("Invalid/Expired Access Token.");

                    RefreshTokens(refreshToken);

                    if (oauthTokens.ContainsKey("accessToken") && oauthTokens.ContainsKey("refreshToken"))
                    {
                        Output("Refreshed Access Token");
                        return CreateEntity(oauthTokens["accessToken"], realmID, oauthTokens["refreshToken"], entityType, XML, IsProd);
                    }
                    else
                    {
                        Output("Error while refreshing tokens");
                        return "REVOKE";
                    }
                }
                else if (ex.Message == "429")
                {
                    Output("Too Many Concurrent Requests - Retrying in 1 minute...");
                    System.Threading.Thread.Sleep(60000);
                    return CreateEntity(accessToken, realmID, refreshToken, entityType, XML, IsProd);
                }
                else
                {
                    Output("IdsException = " + ex.Message);
                    return ex.Message;
                }
            }
            catch (Exception ex)
            {
                Output("Exception = " + ex.Message);
                return ex.Message;
            }
            return "";
        }

        private async System.Threading.Tasks.Task GetAuthTokensAsync(string code)
        {
            var tokenResponse = await oauth2Client.GetBearerTokenAsync(code);

            if (!oauthTokens.ContainsKey("accessToken"))
                oauthTokens.Add("accessToken", tokenResponse.AccessToken);
            else
                oauthTokens["accessToken"] = tokenResponse.AccessToken;

            if (!oauthTokens.ContainsKey("accessTokenExpiry"))
                oauthTokens.Add("accessTokenExpiry", tokenResponse.AccessTokenExpiresIn.ToString());
            else
                oauthTokens["accessTokenExpiry"] = tokenResponse.AccessTokenExpiresIn.ToString();

            if (!oauthTokens.ContainsKey("refreshToken"))
                oauthTokens.Add("refreshToken", tokenResponse.RefreshToken);
            else
                oauthTokens["refreshToken"] = tokenResponse.RefreshToken;

            if (!oauthTokens.ContainsKey("refreshTokenExpiry"))
                oauthTokens.Add("refreshTokenExpiry", tokenResponse.RefreshTokenExpiresIn.ToString());
            else
                oauthTokens["refreshTokenExpiry"] = tokenResponse.RefreshTokenExpiresIn.ToString();
        }

        private ServiceContext BuildServiceContext(string accessToken, string realmID, bool IsProd)
        {
            OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(accessToken);
            ServiceContext serviceContext = new ServiceContext(realmID, IntuitServicesType.QBO, oauthValidator);
            if (IsProd)
                serviceContext.IppConfiguration.BaseUrl.Qbo = "https://quickbooks.api.intuit.com/";
            else
                serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
            serviceContext.IppConfiguration.MinorVersion.Qbo = "41";
            serviceContext.IppConfiguration.Message.Request.SerializationFormat = SerializationFormat.Xml;
            serviceContext.IppConfiguration.Message.Response.SerializationFormat = SerializationFormat.Xml;

            return serviceContext;
        }

        private void Output(string logMsg)
        {
            if (IsDebugging)
            {
                StreamWriter sw = File.AppendText(@"C:\OAuthQBOnlineDebugging.log");
                try
                {
                    string logLine = string.Format("{0:G}: {1}", DateTime.Now, logMsg);
                    sw.WriteLine(logLine);
                }
                finally
                {
                    sw.Close();
                }
            }
        }
    }
}
