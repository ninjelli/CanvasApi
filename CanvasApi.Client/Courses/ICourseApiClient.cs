﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CanvasApi.Client.Courses.Models;

namespace CanvasApi.Client.Courses
{
    public interface ICourseApiClient
    {
        /// <summary>
        /// Returns the paginated list of active courses for the current user.
        /// </summary>
        /// <param name="optionsFactory"></param>
        /// <returns></returns>
        Task<IEnumerable<ICourse>> List(Action<IListOptions> optionsFactory);

        /// <summary>
        /// Returns a paginated list of active courses for this user. To view the course list for a user other than yourself, you must be either an observer of that user or an administrator
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="optionsFactory"></param>
        /// <returns></returns>
        Task<IEnumerable<ICourse>> List(long userId, Action<IListOptionsBasic> optionsFactory);

        /// <summary>
        /// Return progress information for the user and course
        ///
        /// You can supply self as the user_id to query your own progress in a course.To query another user's progress, you must be a teacher in the course, an administrator, or a linked observer of the user.
        /// </summary>
        /// <param name="courseId">The Id of the course</param>
        /// <param name="userId">The Id of the user record. For Self set the user id to a negative number</param>
        /// <returns></returns>
        Task<ICourseProgress> Progress(long courseId, long userId);

        Task<ICourse> Get(long id);
    }
}
