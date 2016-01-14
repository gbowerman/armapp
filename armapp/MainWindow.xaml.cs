// ARM app
// Simple Azure Resource Manager app to make REST calls
// Written by: @gbowerman
// Date: 12/30/15

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Deployment.Application;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;

namespace armapp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string BASEAPI = "2015-01-01"; // API version for resource group calls
        const string VMSSAPI = "2015-06-15"; // API version for VM Scale Set calls
        const string ARM_ENDPOINT = "https://management.azure.com/";

        string tenantId = null;
        string clientId = null; // same as applicationId
        string subscriptionId = null;
        string clientSecret = null;

        public MainWindow()
        {
            InitializeComponent();

            // display version
            mainWindow.Title = "ARM App " + getRunningVersion();

            // check for saved settings
            if (Properties.Settings.Default.subscriptionID != null)
            {
                subscriptionId = Properties.Settings.Default.subscriptionID;
                subscriptionIDBox.Text = subscriptionId;
            }

            if (!String.IsNullOrEmpty(Properties.Settings.Default.clientID) &&
                    !String.IsNullOrEmpty(Properties.Settings.Default.clientSecret) &&
                    !String.IsNullOrEmpty(Properties.Settings.Default.tenantID))
            {
                clientId = Properties.Settings.Default.clientID;
                clientIDBox.Text = clientId;

                tenantId = Properties.Settings.Default.tenantID;
                tenantIDBox.Text = tenantId;

                clientSecret = Properties.Settings.Default.clientSecret;
            }
            else
            {
                MessageBox.Show("If you're running this app for the first time, start by saving a valid client ID, client secret and tenant ID\n" +
                                "in the Setup tab. You can optionally save a default subscription ID too.\n", "Welcome",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                tabControl.SelectedItem = setupTab;
            }
        }

        /// <summary>
        /// In case user wants to view/copy their access token
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void getAccessTokenButton_Click(object sender, RoutedEventArgs e)
        {
            string accessToken = GetAccessToken();
            outputBox.Text = accessToken;
        }

        /// <summary>
        /// Acquire an authentication token using Azure app client credentials
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {
            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId: clientId, clientSecret: clientSecret);
            var result = authenticationContext.AcquireToken(resource: ARM_ENDPOINT, clientCredential: credential);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            string token = result.AccessToken;
            return token;
        }

        /// <summary>
        /// User sends a REST GET request to the Azure endpoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void getButton_Click(object sender, RoutedEventArgs e)
        {
            string getURL = urlBox.Text;
            string response = doGET(getURL, GetAccessToken());
            outputBox.Text = response;
        }

        /// <summary>
        /// User sends a REST PUT request to the Azure endpoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void putButton_Click(object sender, RoutedEventArgs e)
        {
            string putURL = urlBox.Text;
            string body = outputBox.Text;
            string response = doPUT(putURL, body, GetAccessToken());
            outputBox.Text = response;
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            string deleteURL = urlBox.Text;
            string response = doDELETE(deleteURL, GetAccessToken());
            outputBox.Text = response;
        }

        /// <summary>
        /// Auto-fill base URL using the user's default subscription id
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void baseButton_Click(object sender, RoutedEventArgs e)
        {
            // check that user has set a default subscription value
            if (String.IsNullOrEmpty(subscriptionId))
            {
                MessageBox.Show("Set a value for your default subscription ID in the Setup tab first.", "No default subscription ID",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            urlBox.Text = ARM_ENDPOINT + "subscriptions/" + subscriptionId + "/";
        }

        /// <summary>
        /// Add ResourceGroups to the URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rgButton_Click(object sender, RoutedEventArgs e)
        {
            if (!urlBox.Text.StartsWith(ARM_ENDPOINT + "subscriptions/"))
            {
                MessageBox.Show("Add a Base URL and subscription before adding this resource", "Invalid Base URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            urlBox.Text += "resourceGroups?api-version=" + BASEAPI;
        }

        /// <summary>
        /// Adds Azure Compute provider virtualMachineScaleSets resource to the URL. Assumes there is a Resource Group already in the URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vmssButton_Click(object sender, RoutedEventArgs e)
        {
            if (!urlBox.Text.StartsWith(ARM_ENDPOINT + "subscriptions/"))
            {
                MessageBox.Show("Add a Base URL and subscription before adding this resource", "Invalid Base URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // remove any existing API version
            if (urlBox.Text.Contains("?api-version"))
            {
                int removeIdx = urlBox.Text.IndexOf('?');
                urlBox.Text = urlBox.Text.Remove(removeIdx);
            }
            if (!urlBox.Text.Contains("/resourceGroups/"))
            {
                MessageBox.Show("Add resourceGroups/<your resource group> to the base URL", "Invalid resource group", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // make sure there is a forward slash separator
            if (!urlBox.Text.EndsWith("/"))
            {
                urlBox.Text += "/";
            }
            urlBox.Text += "providers/Microsoft.Compute/virtualMachineScaleSets?api-version=" + VMSSAPI;
        }

        /// <summary>
        /// Append an API version to the URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void apiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!urlBox.Text.StartsWith(ARM_ENDPOINT + "subscriptions/"))
            {
                MessageBox.Show("Add a Base URL and subscription before adding this resource", "Invalid Base URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // remove any existing API version
            if (urlBox.Text.Contains("?api-version"))
            {
                int removeIdx = urlBox.Text.IndexOf('?');
                urlBox.Text = urlBox.Text.Remove(removeIdx);
            }
            urlBox.Text += "?api-version=" + BASEAPI;
        }

        /// <summary>
        /// Clear the output/body text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            outputBox.Text = "";
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (tenantIDBox.Text.Length > 10)
            {
                tenantId = tenantIDBox.Text;
                Properties.Settings.Default.tenantID = tenantId;
            }
            if (clientIDBox.Text.Length > 10)
            {
                clientId = clientIDBox.Text;
                Properties.Settings.Default.clientID = clientId;
            }
            if (clientSecretBox.SecurePassword.Length > 1)
            {
                clientSecret = clientSecretBox.Password;
                Properties.Settings.Default.clientSecret = clientSecret;
            }
            if (subscriptionIDBox.Text.Length > 10)
            {
                subscriptionId = subscriptionIDBox.Text;
                Properties.Settings.Default.subscriptionID = subscriptionId;
            }
            Properties.Settings.Default.Save();
            MessageBox.Show("Application settings saved.", "Settings saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Makes a GET request to the designated URI, authenticate using token, returning result as text
        /// </summary>
        /// <param name="URI"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string doGET(string URI, String token)
        {
            Uri uri = new Uri(String.Format(URI));

            // Create the request
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";

            // Get the response
            HttpWebResponse httpResponse = null;
            try
            {
                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error from : " + uri + ": " + ex.Message,
                                "HttpWebResponse exception", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            string result = null;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        /// <summary>
        /// Makes a PUT request to the specified URI, authenticating using token, passing body
        /// </summary>
        /// <param name="URI"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private string doPUT(string URI, string body, String token)
        {
            Uri uri = new Uri(String.Format(URI));

            // Create the request
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "PUT";

            try
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(body);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting up stream writer: " + ex.Message,
                    "GetRequestStream exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Get the response
            HttpWebResponse httpResponse = null;
            try
            {
                httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error from : " + uri + ": " + ex.Message,
                                "HttpWebResponse exception", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            string result = null;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        /// <summary>
        /// Makes a DELETE request to the designated URI, authenticate using token, returning result as text
        /// </summary>
        /// <param name="URI"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string doDELETE(string URI, String token)
        {
            Uri uri = new Uri(String.Format(URI));

            // Create the request
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "DELETE";

            // Get the response
            HttpWebResponse httpResponse = null;
            try
            {
                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error from : " + uri + ": " + ex.Message,
                                "HttpWebResponse exception", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            string result = null;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        /// <summary>
        /// Standard method to get and display version
        /// </summary>
        /// <returns></returns>
        private Version getRunningVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch (Exception)
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }

        }
    }
}
