﻿// Copyright (C) 2012, Tom Adriaenssen
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS 
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace Inferis.Location
{
    public class GoogleAddressResolver : ICivicAddressResolver
    {
        public CivicAddress ResolveAddress(GeoCoordinate coordinate)
        {
            var signal = new ManualResetEvent(false);
            var address = new CivicAddress();
            Exception error = null;
            ResolveAddressAsync(coordinate, args => {
                try {
                    if (args.Error != null) {
                        error = args.Error;
                        return;
                    }

                    address = ParseResult(args);
                }
                catch (Exception ex) {
                    error = ex;
                }
                finally {
                    signal.Set();
                }
            });

            signal.WaitOne();
            if (error != null) throw error;
            return address;
        }

        private static CivicAddress ParseResult(DownloadStringCompletedEventArgs args)
        {
            try {
                if (!string.IsNullOrEmpty(args.Result)) {
                    var doc = XDocument.Parse(args.Result);
                    if (doc.Root == null)
                        return new CivicAddress();

                    var status = doc.Root.Descendants("status").Select(x => x.Value).FirstOrDefault();
                    if (status != "OK")
                        return new CivicAddress();

                    var result = doc.Root.Descendants("result").WithTypeValue("street_address").FirstOrDefault();
                    if (result == null) {
                        result = doc.Root.Descendants("result").WithTypeValue("route").FirstOrDefault();
                        if (result == null)
                            return new CivicAddress();
                    }

                    var components = result.Elements("address_component").ToArray();
                    var addressLine1 = string.Format("{0} {1}",
                        components.WithTypeValue("route").LongNameValue(),
                        components.WithTypeValue("street_number").LongNameValue());
                    var city = string.Format("{0}", components.WithTypeValue("locality").LongNameValue());
                    var stateProvince = string.Format("{0}", components.WithTypeValue("administrative_area_level_1").LongNameValue());
                    var postalCode = string.Format("{0}", components.WithTypeValue("postal_code").LongNameValue());
                    var country = string.Format("{0}", components.WithTypeValue("country").LongNameValue());

                    return new CivicAddress(addressLine1, "", "", city, country, "", postalCode, stateProvince);
                }
            }
            catch (Exception) {

                throw;
            }

            return new CivicAddress();
        }

        public void ResolveAddressAsync(GeoCoordinate coordinate)
        {
            ResolveAddressAsync(coordinate, args => {
                var error = args.Error;
                var address = new CivicAddress();
                try {
                    if (!args.Cancelled && args.Error == null) {
                        address = ParseResult(args);
                    }
                }
                catch (Exception ex) {
                    error = ex;
                }
                finally {
                    var handler = ResolveAddressCompleted;
                    if (handler != null) {
                        handler(this, new ResolveAddressCompletedEventArgs(address, error, args.Cancelled, null));
                    }
                }
            });
        }

        public void ResolveAddressAsync(GeoCoordinate coordinate, Action<DownloadStringCompletedEventArgs> handler)
        {
            if (coordinate == null)
                throw new ArgumentNullException("coordinate");

            var url = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=true", coordinate.Latitude, coordinate.Longitude);
            var wc = new WebClient();
            var timeout = new ManualResetEvent(false);
            wc.DownloadStringCompleted += (sender, args) => {
                timeout.Set();
                handler(args);
            };
            wc.DownloadStringAsync(new Uri(url));
            ThreadPool.QueueUserWorkItem(o => {
                if (!timeout.WaitOne(30000))
                    wc.CancelAsync();
            });
        }

        public event EventHandler<ResolveAddressCompletedEventArgs> ResolveAddressCompleted;
    }

    internal static class GoogleAddressResolveXmlExtensions
    {
        public static IEnumerable<XElement> WithTypeValue(this IEnumerable<XElement> elements, string type)
        {
            return elements.Where(c => c.Elements("type").Any(x => x.Value == type));
        }

        public static string LongNameValue(this IEnumerable<XElement> elements)
        {
            return elements.Select(x => x.Element("long_name")).Where(x => x != null).Select(x => x.Value).FirstOrDefault();
        }
    }

}
