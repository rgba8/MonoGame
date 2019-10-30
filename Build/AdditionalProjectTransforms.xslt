<xsl:if test="$root/Input/Generation/Platform = 'Android' or $root/Input/Generation/Platform = 'Ouya'">
	<Import Project="$(MSBuildExtensionsPath)\Xamarin\Android\Xamarin.Android.CSharp.targets" />
	<PropertyGroup>
		<AndroidSupportedAbis>armeabi-v7a,arm64-v8a,x86,x86_64</AndroidSupportedAbis>
	</PropertyGroup>
</xsl:if>
