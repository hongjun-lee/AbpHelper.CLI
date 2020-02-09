﻿using System.Collections.Generic;
using System.Linq;
using AbpHelper.Extensions;
using AbpHelper.Generator;
using AbpHelper.Models;
using AbpHelper.Steps;
using AbpHelper.Steps.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AbpHelper.Workflow.Abp
{
    public static class UIRazorPagesGenerationWorkflow
    {
        public static IActivityBuilder AddUIRazorPagesGenerationWorkflow(this IActivityBuilder builder)
        {
            return builder
                    /* Generate razor pages ui files*/
                    .Then<TemplateGroupGenerationStep>(
                        step =>
                        {
                            step.Model = new
                            {
                                EntityInfo = step.Get<EntityInfo>(),
                                ProjectInfo = step.Get<ProjectInfo>()
                            };
                            step.GroupName = "UIRazor";
                            step.TargetDirectory = step.GetParameter<string>("BaseDirectory");
                        }
                    )
                    /* Add menu */
                    .Then<FileFinderStep>(
                        step => step.SearchFileName = "*MenuContributor.cs"
                    )
                    .Then<ModificationCreatorStep>(
                        step =>
                        {
                            var entityInfo = step.Get<EntityInfo>();
                            step.ModificationBuilders = new List<ModificationBuilder>
                            {
                                new InsertionBuilder(root => root.Descendants<MethodDeclarationSyntax>()
                                        .Single(n => n.Identifier.ToString() == "ConfigureMainMenuAsync")
                                        .GetEndLine(),
                                    TextGenerator.GenerateByTemplateName("MenuContributor_AddMenuItem", new {EntityInfo = entityInfo})
                                )
                            };
                        })
                    .Then<FileModifierStep>()
                    /* Add mapping */
                    .Then<FileFinderStep>(
                        step => step.SearchFileName = "*WebAutoMapperProfile.cs"
                    )
                    .Then<ModificationCreatorStep>(
                        step =>
                        {
                            var entityInfo = step.Get<EntityInfo>();
                            step.ModificationBuilders = new List<ModificationBuilder>
                            {
                                new InsertionBuilder(
                                    root => root.Descendants<UsingDirectiveSyntax>().Last().GetEndLine(),
                                    GetEntityDtoUsingText(step),
                                    modifyCondition: root => root.DescendantsNotContain<UsingDirectiveSyntax>(GetEntityDtoUsingText(step))
                                ),
                                new InsertionBuilder(
                                    root => root.Descendants<ConstructorDeclarationSyntax>().Single().GetEndLine(),
                                    TextGenerator.GenerateByTemplateName("WebAutoMapperProfile_CreateMap", new {EntityInfo = entityInfo})
                                )
                            };
                        })
                    .Then<FileModifierStep>()
                    /* Add localization */
                    .Then<FileFinderStep>(
                        step => step.Multiple = true,
                        step => step.SearchFileName = "*.json",
                        step =>
                        {
                            var projectInfo = step.Get<ProjectInfo>();
                            step.BaseDirectory = $@"{projectInfo.BaseDirectory}\src\{projectInfo.FullName}.Domain.Shared\Localization";
                        }
                    )
                /*
                .Then<LoopStep<string>>(
                    step => step.LoopOn = () => step.GetParameter<string[]>(FileFinderStep.DefaultFilesParameterName),
                    step => step.LoopBody = file =>
                    {
                        
                    }
                    )
                */
                ;
        }

        private static string GetEntityUsingText(Step step)
        {
            return step.GetParameter<string>("EntityUsingText");
        }

        private static string GetEntityDtoUsingText(Step step)
        {
            return step.GetParameter<string>("EntityDtoUsingText");
        }
    }
}