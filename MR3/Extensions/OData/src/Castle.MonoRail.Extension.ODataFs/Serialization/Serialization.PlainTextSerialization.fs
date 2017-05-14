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
open System.IO
open System.Xml
open System.Data.Services.Providers

module PlainTextSerialization = 
    begin

        let internal write_primitive_value (rt:ResourceType) (prop:ResourceProperty) value (writer:TextWriter) = 
            if value = null 
            then writer.Write "null"
            else writer.Write (value.ToString())            

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
                    raise(NotImplementedException()) 

                override x.SerializePrimitive(wrapper, svcBaseUri, containerUri, rt, prop, value, writer, enc) = 
                    write_primitive_value rt prop value writer 
            }

    end