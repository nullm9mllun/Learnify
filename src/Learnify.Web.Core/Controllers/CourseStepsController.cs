﻿using AutoMapper.Internal.Mappers;
using Learnify.Courses;
using Learnify.Courses.Dto;
using Learnify.Dtos.Course;
using Learnify.Models.Courses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Controllers
{
    [Route("api/[controller]")]
    public class CourseStepsController: LearnifyControllerBase
    {
        private readonly ICourseStepAppService _courseStepService;
        private readonly ICourseAppService _courseService;

        public CourseStepsController(ICourseStepAppService courseStepAppService,ICourseAppService courseAppService)
        {
            _courseStepService = courseStepAppService;
            _courseService = courseAppService;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _courseService.GetByIdAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return Ok(ObjectMapper.Map<CourseDto>(course));
        }

        [HttpGet("{courseId:int}/coursesteps")]
        public async Task<IActionResult> GetCourseSteps([FromRoute] int courseId)
        {
            var course = await _courseService.GetByIdAsync(courseId);

            if (course == null)
                return NotFound("Course Not Found");

            var courseStp = await _courseStepService.GetCourseStepsAsync(courseId);

            if (courseStp == null)
            {
                return NotFound("Course has no steps yet.");
            }

            var courseStepsDto = courseStp
                .Select(c => ObjectMapper.Map<CourseStepDto>(c)).ToList();

            return Ok(new CourseStepsOutout
            {
                CourseName = course.CourseName,
                CourseCode = course.CourseCode,
                CourseSteps = courseStepsDto
            });
        }

        [HttpPost("{courseId:int}/coursestep")]
        public async Task<IActionResult> CreateCourseStep([FromRoute] int courseId, [FromBody] courseStepInput input)
        {
            var course = _courseService.GetByIdAsync(courseId);

            if (course == null)
                return NotFound("Course Not Found");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var csMap = ObjectMapper.Map<CourseStep>(input);
            csMap.CourseId = courseId;

            await _courseStepService.CreateAsync(csMap);

            return Ok("Coursestep Created Successfully");
        }

        [HttpPut("{courseStepId:int}/coursestep")]
        public async Task<IActionResult> UpdateCourseStep([FromRoute] int courseStepId, [FromBody] courseStepInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var csMap = ObjectMapper.Map<CourseStep>(input);

            var result = await _courseStepService.UpdateAsync(courseStepId, csMap);

            if (result == null)
            {
                return NotFound();
            }
            return Ok();
        }

        [HttpDelete("{courseStepId:int}/coursestep")]
        public async Task<IActionResult> DeleteCourseStep(int courseStepId)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _courseStepService.DeleteAsync(courseStepId);

            if (result == null)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
