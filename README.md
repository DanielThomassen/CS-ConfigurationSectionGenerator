# CS-ConfigurationSectionGenerator
Code library and tool for generating configuration sections in .NET

## Usage
```batch
ConfigurationGenerator.exe -i <templatefile> -o <outputfile>
ConfigurationGenerator.exe -i <templatefile> -o <outputdirectory> -m

Options
  -i --input           Xml file to use as a templatefile
  -o --output          File or directory to output to.
  -m --MultipleFiles   Split output into a separate file for each class.
```

## Examples
### Simple configration with several elements
```XML
<SimpleConfigurationSection>
    <Font color="#000cd1" family="Times new roman" background="#FF0000" />
    <BackgroundImage src="images/bg.jpg" />
</SimpleConfigurationSection>
```
As you can see in this example you can add regular xml elements to the root element. 

### Collections
It isn't possible to have a collection of items in the configuration section root element

```Xml
<InvalidConfigurationSection>
    <Task time="13:04" id="12" />
    <Task time="15:00" id="33" />
    <Task time="01:00" id="8" />
</InvalidConfigurationSection>
```
Trying to generate code from this XML will result in the application failing. You should instead use the following XML.
```XML
<ValidConfigurationSection>
    <Tasks>
        <Task time="13:04" id="12" />
        <Task time="15:00" id="33" />
        <Task time="01:00" id="8" />
    </Tasks>
</ValidConfigurationSection>
```

You can also generate complex structures containing sub-elements or nested collections e.g.
```XML
<EndPointSection>
    <EndpointTypes>
        <Endpoint languageCode="nl" id="1" authorizationKey="exampleKey1">
            <Autocomplete url="http://google.com/" />
            <Navigation url="http://tomtom.nl/" />
            <Search url="http://bing.com/" />
			<searchTerms>
				<term value="term1" />
				<term value="term2" />
			</searchTerms>
        </Endpoint>
        <Endpoint languageCode="en" id="2" authorizationKey="exampleKey2" />
    </EndpointTypes>
</EndPointSection>
```

As can be seen in the above example you aren't required to write out the sub-elements for each EndPoint.
This is because the ConfigurationGenerator will use the first element to determine the structure of the generated class.
This means that you the first element NEEDS to contain all possible attributes and elements.

Also note that in order to recognize something as a collection it should have 2 or more elements with the same name. 

## Known issues / limitations
- Validation hasn't been implemented yet
- No way of defining attribute types (everything is a string)
- Cannot have a ConfigurationSection which has '.' in it's name
- Cannot configure the namespace for the generated classes
- Cannot have elements with the same name within the structure e.g.
```XML
<MySection>
    <WorkItems>
        <Item task="clean"  />
    </WorkItems>
    <Inventory>
        <Item name="box" />
    </Inventory>
</MySection>
```