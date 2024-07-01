1. Create xml file with processed message, i.e. (call it WDProcessStarted.xml):
	<?xml version="1.0"?>
	<rcs:Message 
		xmlns:xs="http://www.w3.org/2001/XMLSchema"
		xmlns:rcs="http://www.transport.bombardier.com/2014/RcsCmm"
		xmlns:rcswds="http://www.transport.bombardier.com/2014/RcsCmm/wds"
		xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
		xsi:schemaLocation="file://d://ext-sources//tmsra_core//ref-binaries//xsd//RcsCmmWds.xsd">	

		<hdr>
			<source>TMSRASomething</source>   			 							 	<!-- sender, processId, RATOIF in this case -->
			<messageId>rcs.e2k.ctc.tmsraSomething-1234</messageId>	<!-- not mandatory --> 
			<content>rcswds:processStarted</content>   			<!-- not mandatory --> 
		</hdr>
		
		<data>
			<rcswds:processStarted sourceId="TMSRASomething"/>
		</data>
	</rcs:Message>
2. run
	xsd WDProcessStarted.xml
	It will generate 2 files - WDProcessStarted.xsd and WDProcessStarted_app1.xsd
3. Open both files and copy whole "<xs:element name="processStarted"" element from WDProcessStarted_app1.xsd to end of WDProcessStarted.xsd file
4. From WDProcessStarted.xsd remove:
	a) xmlns:app1="http://www.transport.bombardier.com/2014/RcsCmm/wds"
	b) <xs:import namespace="http://www.transport.bombardier.com/2014/RcsCmm/wds" schemaLocation="WDProcessStarted_app1.xsd" />
	c) prefix app1 from "ref="app1:processStarted"" element (leave only ref="processStarted")
5. Inspect xsd to find some modifications to be made manually (like not-mandatory fields)
6. run
	xsd WDProcessStarted.xsd /classes /namespace:XSD /o:.
	to inspect that new .cs file is generated properly