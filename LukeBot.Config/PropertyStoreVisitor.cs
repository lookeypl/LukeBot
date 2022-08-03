namespace LukeBot.Config
{
    internal interface PropertyStoreVisitor
    {
        void Visit<T>(PropertyType<T> p);
        void VisitStart(PropertyDomain pd);
        void VisitEnd(PropertyDomain pd);
    }
}