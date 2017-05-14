//  Copyright 2004-2012 Castle Project - http://www.castleproject.org/
//  Hamilton Verissimo de Oliveira and individual contributors as indicated. 
//  See the committers.txt/contributors.txt in the distribution for a 
//  full listing of individual contributors.
// 
//  This is free software; you can redistribute it and/or modify it
//  under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 3 of
//  the License, or (at your option) any later version.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this software; if not, write to the Free
//  Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
//  02110-1301 USA, or see the FSF site: http://www.fsf.org.

namespace Castle.MonoRail.Extension.OData

open System
open System.Xml
open System.Data.Services.Providers

module XmlSerialization = 
    begin
        let internal to_xml_string (valType:Type) (originalVal:obj) = 
            let targetType = 
                let nulType = Nullable.GetUnderlyingType(valType)
                if nulType = null then valType else nulType
            if   targetType = typeof<string>   then string(originalVal)
            elif targetType = typeof<bool>     then XmlConvert.ToString(originalVal :?> bool)
            elif targetType = typeof<float>    then XmlConvert.ToString(originalVal :?> float)
            elif targetType = typeof<double>   then XmlConvert.ToString(originalVal :?> double)
            elif targetType = typeof<int8>     then XmlConvert.ToString(originalVal :?> int8)
            elif targetType = typeof<int16>    then XmlConvert.ToString(originalVal :?> int16)
            elif targetType = typeof<int32>    then XmlConvert.ToString(originalVal :?> int32)
            elif targetType = typeof<int64>    then XmlConvert.ToString(originalVal :?> int64)
            elif targetType = typeof<DateTime> then XmlConvert.ToString(originalVal :?> DateTime, XmlDateTimeSerializationMode.RoundtripKind)
            elif targetType = typeof<decimal>  then XmlConvert.ToString(originalVal :?> decimal)
            elif targetType = typeof<byte[]>   then Convert.ToBase64String(originalVal :?> byte[])
            elif targetType = typeof<byte>     then XmlConvert.ToString(originalVal :?> byte)
            else raise(InvalidOperationException("primitive value conversion to its xml representation is not supported. " + valType.FullName))

        let internal write_primitive_value (rt:ResourceType) (prop:ResourceProperty) value (writer:XmlWriter) = 
            let name = prop.Name

            writer.WriteStartElement (name, "http://schemas.microsoft.com/ado/2007/08/dataservices")

            let typename = rt.FullName

            if typename <> "Edm.String" then 
                writer.WriteAttributeString ("type", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata", typename)
            
            if value = null 
            then writer.WriteAttributeString("null", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata", "true")
            else writer.WriteString(value.ToString())            

            writer.WriteEndElement()

        let private write_primitive (rt:ResourceType) (prop:ResourceProperty) (value:obj) (txtwriter) (enc) = 
            use writer = SerializerCommons.create_xmlwriter (txtwriter) enc
            write_primitive_value rt prop value writer


        let CreateDeserializer () = 
            { new Deserializer() with 
                override x.DeserializeMany (rt, reader, enc) = 
                    // read_feed rt reader enc
                    raise(NotImplementedException())
                override x.DeserializeSingle (rt, reader, enc) = 
                    // read_item rt reader enc
                    raise(NotImplementedException())
            }

        let CreateSerializer () = 
            { new Serializer() with 
                override x.SerializeMany(wrapper, svcBaseUri, containerUri , rt, items, writer, enc) = 
                    // write_items wrapper svcBaseUri containerUri rt items writer enc
                    raise(NotImplementedException()) 

                override x.SerializeSingle(wrapper, svcBaseUri, containerUri , rt, items, writer, enc) = 
                    ()

                override x.SerializePrimitive(wrapper, svcBaseUri, containerUri, rt, prop, value, writer, enc) = 
                    // write_item wrapper svcBaseUri containerUri rt item writer enc 
                    // raise(NotImplementedException())
                    write_primitive rt prop value writer enc
            }

    end