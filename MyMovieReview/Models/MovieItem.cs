using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMovieReview.Models
{
    class MovieItem
    {
        //id由数据库自增赋值，通过count使ViewModel中的id与数据库中的id相同
        private int id;

        //name of the movie
        public string title { get; set; }

        //short description
        public string description { get; set; }

        //points of the movie
        public double rank { get; set; }

        //my movie review
        public string review { get; set; }

        //count my movie id
        public static int count = 0;

        //缺少id的构造函数
        public MovieItem(string title, string description, double rank, string review)
        {
            count++;
            this.id = count;
            this.title = title;
            this.description = description;
            this.rank = rank;
            this.review = review;
        }

        //带id的构造函数
        public MovieItem(int id, string title, string description, double rank, string review)
        {
            this.id = id;
            //使得count和当前id保持一致
            if (id > count) count = id;
            this.title = title;
            this.description = description;
            this.rank = rank;
            this.review = review;
        }

        //获取某电影项的对应id
        public int getId()
        {
            return id;
        }

        //给电影项的id赋值
        public void setId(int id)
        {
            this.id = id;
        }
    }
}
