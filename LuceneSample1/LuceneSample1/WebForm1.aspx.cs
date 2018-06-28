using System;
using System.Data;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace LuceneSample1
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Page.IsPostBack)
            {
                GridView1.DataSource = Sample;
                GridView1.DataBind();
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            var query = TextBox1.Text.Trim();

            var results = search(query);

            GridView1.DataSource = results;
            GridView1.DataBind();

        }

        DataTable Sample
        {
            get
            {
                var ds = new DataSet();
                ds.ReadXml(Server.MapPath("~/App_data/dataset.xml"));
                return ds.Tables[0];
            }
        }

        #region Search Methods

        Directory createIndex(DataTable table)
        {
            var directory = new RAMDirectory();

            using (Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            using (var writer = new IndexWriter(directory, analyzer, new IndexWriter.MaxFieldLength(1000)))
            { // the writer and analyzer will popuplate the directory with documents

                foreach (DataRow row in table.Rows)
                {
                    var document = new Document();

                    document.Add(new Field("FirstName",row["FirstName"].ToString(),Field.Store.YES,Field.Index.ANALYZED));
                    document.Add(new Field("LastName", row["LastName"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("JobTitle", row["JobTitle"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("BirthDate", row["BirthDate"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("ID", row["ID"].ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));

                    var date = row["BirthDate"].ToString();

                    date = string.Format("{0} {1}", date, date.Replace("/", " / "));

                    document.Add(new Field("FullText",
                        string.Format("{0} {1} {2} {3} {4}", row["FirstName"], row["LastName"], row["JobTitle"], date, row["ID"])
                        , Field.Store.YES, Field.Index.ANALYZED));



                    writer.AddDocument(document);
                }

                writer.Optimize();
                writer.Flush(true, true, true);
            }

            return directory;
        }

        DataTable search(string textSearch)
        {

            var table = Sample.Clone();

            var Index = createIndex(Sample);

            using (var reader = IndexReader.Open(Index, true))
            using (var searcher = new IndexSearcher(reader))
            {
                using (Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
                {
                    var queryParser = new QueryParser(Version.LUCENE_30, "FullText", analyzer);

                    queryParser.AllowLeadingWildcard = true;

                    var query = queryParser.Parse(textSearch);

                    var collector = TopScoreDocCollector.Create(1000, true);

                    searcher.Search(query, collector);

                    var matches = collector.TopDocs().ScoreDocs;

                    foreach (var item in matches)
                    {
                        var id = item.Doc;
                        var doc = searcher.Doc(id);

                        var row = table.NewRow();

                        row["FirstName"] = doc.GetField("FirstName").StringValue;
                        row["LastName"] = doc.GetField("LastName").StringValue;
                        row["JobTitle"] = doc.GetField("JobTitle").StringValue;
                        row["JobTitle"] = doc.GetField("JobTitle").StringValue;
                        row["BirthDate"] = doc.GetField("BirthDate").StringValue;
                        row["ID"] = doc.GetField("ID").StringValue;

                        table.Rows.Add(row);

                    }
                }
            }

            return table;

        }

        #endregion
    }
}