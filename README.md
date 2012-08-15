GoogleAddressResolver
=====================

Since Windows Phone 7 has a non-functional CivicAddressResolver (the class itself works, but it always returns *not found* so it's unusable), and I needed something to resolve geolocations into usable addresses, I decided to write a class that uses the [Google geocoder](https://developers.google.com/maps/documentation/geocoding/) to resolve a coordinate.

# Usage

Just use it like you would use CivicAddressResolver. The class `GoogleAddressResolver` implements the interface `ICivicAddressResolver` just like `CivicAddressResolver`, so its usage is exactly the same.

    var resolver = new GoogleAddressResolver();
    resolver.ResolveAddressCompleted += (o, args) => {
        if (args.Address == null || args.Address.IsUnknown) {
        	// Found it
        }
        else {
        	// Not found. Bummer. :(
        }
    };
    resolver.ResolveAddressAsync(geolocation);

You can use it as a fallback for CivicAddressResolver like this:

    ICivicAddressResolver resolver = new GoogleAddressResolver();
    resolver.ResolveAddressCompleted += (o, args) => {
        if (args.Address == null || args.Address.IsUnknown) {
        	DoStuff();
        }
        else {
		    resolver = new GoogleAddressResolver();
		    resolver.ResolveAddressCompleted += (o, args) => {
		        if (args.Address == null || args.Address.IsUnknown) {
		        	DoStuff();
		        }
		        else {
		        	ShowError();
		        }
		    };
		    resolver.ResolveAddressAsync(geolocation);
        }
    };
    resolver.ResolveAddressAsync(geolocation);

This has the benefit that when/if Microsoft decides to implement `CivicAddressResolver`, it will be used instead of `GoogleAddressResolver` which has its downsides (see: Caveats).

# Caveats

Since this uses the [Google Geocoding API](https://developers.google.com/maps/documentation/geocoding/), which states: 
	
> Note: the Geocoding API may only be used in conjunction with a Google map; geocoding results without displaying them on a map is prohibited. For complete details on allowed usage, consult the Maps API Terms of Service License Restrictions.'

This means that you're only allowed to use the geocoder API in conjunction with Google Maps. If you don't: proceed at your own risk. 

# Usage Limits

The geocoder API has a limit of 2500 calls per day. That should be enough for any casual use in a mobile app.

# Real world usage

This component is used in the Windows Phone 7 version of [Drash](http://dra.sh).
Let me know if you use this and want your app included here.

# Credits

No real need to include credits if you use this in your app, but I'd appreciate it if you'd let me know if you used this little piece of software in your app. It's always nice to know where ones code's being used.

# License

**GoogleAddressResolver** is published under the MIT license:

*Copyright (C) 2012, Tom Adriaenssen*

*Permission is hereby granted, free of charge, to any person obtaining a copy of*
*this software and associated documentation files (the "Software"), to deal in*
*the Software without restriction, including without limitation the rights to*
*use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies*
*of the Software, and to permit persons to whom the Software is furnished to do*
*so, subject to the following conditions:*

*The above copyright notice and this permission notice shall be included in all*
*copies or substantial portions of the Software.*

*THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR*
*IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,*
*FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE*
*AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER*
*LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,*
*OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE*
*SOFTWARE.*
