using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Vehicles;

namespace UncreatedWarfare
{
    public class JSONSaver<T>
    {
        protected string directory;

        public JSONSaver(string directory)
        {
            this.directory = directory;
            CreateFileIfNotExists();
        }

        protected void CreateFileIfNotExists()
        {
            if (File.Exists(directory))
            {
                StreamWriter creator = File.CreateText(directory);
                creator.WriteLine("[]");
                creator.Close();
                creator.Dispose();
            }
        }

        protected void AddObjectToSave(object item)
        {
            if (item.GetType() != typeof(T))
            {
                throw new TypeArgumentException(MethodBase.GetCurrentMethod(), item);
            }

            var list = GetExistingObjects();
            list.Add((T)item);

            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            try
            {
                serializer.Serialize(writer, list);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }

        protected void RemoveFromSaveWhere(Predicate<T> match)
        {
            var list = GetExistingObjects();

            list.RemoveAll(match);

            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            try
            {
                serializer.Serialize(writer, list);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }

        protected void RemoveAllObjectsFromSave()
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            try
            {
                serializer.Serialize(writer, new List<T>());
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }

        protected void OverwriteSavedList(List<T> newList)
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            try
            {
                serializer.Serialize(writer, newList);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }

        protected List<T> GetExistingObjects()
        {
            StreamReader r = File.OpenText(directory);

            try
            {
                string json = r.ReadToEnd();
                var list = JsonConvert.DeserializeObject<List<T>>(json);

                r.Close();
                r.Dispose();
                return list;
            }
            catch
            {
                throw new JSONReadException(r, directory);
            }
        }

        protected List<T> GetObjectsWhere(Func<T, bool> predicate)
        {
            return GetExistingObjects().Where(predicate).ToList();
        }

        protected bool ObjectExists(Predicate<T> match, out T item)
        {
            var list = GetExistingObjects();
            item = list.Find(match);
            if (item != null)
                return true;
            else
                return false;
        }
        public bool IsPropertyValid<TEnum>(object name, out TEnum property) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(name.ToString(), out var p))
            {
                property = p;
                return true;
            }
            property = p;
            return false;
        }

        protected class TypeArgumentException : Exception
        {
            public TypeArgumentException() { }

            public TypeArgumentException(MethodBase currentMethod, object badObject)
                : base(string.Format("Arguments of {0} should match the JSONSaver's specified type: {1}. The object you gave it was of type: {2}", currentMethod.Name, typeof(T).Name, badObject.GetType().Name))
            {
                
            }
        }

        protected class JSONReadException : Exception
        {
            public JSONReadException() { }

            public JSONReadException(StreamReader reader, string directory)
                : base(string.Format("Could not deserialize data from {0} because the data was corrupted.", directory))
            {
                reader.Close();
                reader.Dispose();
            }
        }
    }
}
