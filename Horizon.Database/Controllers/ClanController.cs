﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Horizon.Database.DTO;
using Horizon.Database.Models;
using Horizon.Database.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Horizon.Database.Services;
using System.Security.Claims;
using System.Collections;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace Horizon.Database.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClanController : ControllerBase
    {
        private Ratchet_DeadlockedContext db;
        private IAuthService authService;
        public ClanController(Ratchet_DeadlockedContext _db, IAuthService _authService)
        {
            db = _db;
            authService = _authService;
        }

        [Authorize]
        [HttpGet, Route("getActiveClanCountByAppId")]
        public async Task<int> getActiveClanCountByAppId(int AppId)
        {
            var app_id_group = (from a in db.DimAppIds
                                where a.AppId == AppId
                                select a.GroupId).FirstOrDefault();

            var app_ids_in_group = (from a in db.DimAppIds
                                    where (a.GroupId == app_id_group && a.GroupId != null) || a.AppId == AppId
                                    select a.AppId).ToList();

            int accountCount = (from c in db.Clan
                                where app_ids_in_group.Contains(c.AppId)
                                && c.IsActive == true
                                select c).Count();
            return accountCount;
        }

        [Authorize("database")]
        [HttpGet, Route("getClan")]
        public async Task<dynamic> getClan(int clanId)
        {
            AccountService aServ = new AccountService();
            ClanService cs = new ClanService();

            Clan clan = (from c in db.Clan
                           .Include(c => c.ClanMember)
                           .ThenInclude(cm => cm.Account)
                           .Include(c => c.ClanMessage)
                           .Include(c => c.ClanStat)
                           .Include(c => c.ClanCustomStat)
                           .Include(c => c.ClanLeaderAccount)
                           .Include(c => c.ClanInvitation)
                           .ThenInclude(ci => ci.Account)
                         where c.ClanId == clanId && c.IsActive == true
                         select c).FirstOrDefault();

            if (clan == null)
                return NotFound();

            ClanDTO response = new ClanDTO()
            {
                AppId = clan.AppId /*?? 11184*/,
                ClanId = clan.ClanId,
                ClanName = clan.ClanName,
                ClanLeaderAccount = aServ.toAccountDTO(clan.ClanLeaderAccount),
                ClanMemberAccounts = clan.ClanMember.Where(cm => cm.IsActive == true).Select(cm => aServ.toAccountDTO(cm.Account)).ToList(),
                ClanMediusStats = clan.MediusStats,
                ClanWideStats = clan.ClanStat.OrderBy(stat => stat.StatId).Select(cs => cs.StatValue).ToList(),
                ClanCustomWideStats = clan.ClanCustomStat.OrderBy(stat => stat.StatId).Select(cs => cs.StatValue).ToList(),
                ClanMessages = clan.ClanMessage.OrderByDescending(cm => cm.Id).Select(cm => cs.toClanMessageDTO(cm)).ToList(),
                ClanMemberInvitations = clan.ClanInvitation.Select(ci => cs.toClanInvitationDTO(ci)).ToList(),
            };

            return response;
        }

        [Authorize("database")]
        [HttpGet, Route("getClans")]
        public async Task<dynamic> getClans(int appId)
        {
            AccountService aServ = new AccountService();
            ClanService cs = new ClanService();

            List<ClanDTO> clanResponses = new List<ClanDTO>();

            List<Clan> clanList = (from c in db.Clan
                           .Include(c => c.ClanMember)
                           .ThenInclude(cm => cm.Account)
                           .Include(c => c.ClanMessage)
                           .Include(c => c.ClanStat)
                           .Include(c => c.ClanCustomStat)
                           .Include(c => c.ClanLeaderAccount)
                           .Include(c => c.ClanInvitation)
                           .ThenInclude(ci => ci.Account)
                         where c.AppId == appId && c.IsActive == true
                         select c).ToList();

            foreach (var clan in clanList)
            {
                clanResponses.Add(new ClanDTO
                {
                    AppId = clan.AppId,
                    ClanId = clan.ClanId,
                    ClanName = clan.ClanName,
                    ClanLeaderAccount = aServ.toAccountDTO(clan.ClanLeaderAccount),
                    ClanMemberAccounts = clan.ClanMember.Where(cm => cm.IsActive == true).Select(cm => aServ.toAccountDTO(cm.Account)).ToList(),
                    ClanMediusStats = clan.MediusStats,
                    ClanWideStats = clan.ClanStat.OrderBy(stat => stat.StatId).Select(cs => cs.StatValue).ToList(),
                    ClanCustomWideStats = clan.ClanCustomStat.OrderBy(stat => stat.StatId).Select(cs => cs.StatValue).ToList(),
                    ClanMessages = clan.ClanMessage.OrderByDescending(cm => cm.Id).Select(cm => cs.toClanMessageDTO(cm)).ToList(),
                    ClanMemberInvitations = clan.ClanInvitation.Select(ci => cs.toClanInvitationDTO(ci)).ToList(),
                });
            }

            if (clanResponses == null)
                return NotFound();

            return clanResponses;
        }

        [Authorize("database")]
        [HttpGet, Route("searchClanByName")]
        public async Task<dynamic> searchClanByName(string clanName, int appId)
        {
            var app_id_group = (from a in db.DimAppIds
                                where a.AppId == appId
                                select a.GroupId).FirstOrDefault();

            var app_ids_in_group = (from a in db.DimAppIds
                                    where (a.GroupId == app_id_group && a.GroupId != null) || a.AppId == appId
                                    select a.AppId).ToList();

            int clanId = db.Clan.Where(c => c.IsActive == true && app_ids_in_group.Contains(c.AppId) && c.ClanName.ToLower() == clanName.ToLower()).Select(c => c.ClanId).FirstOrDefault();

            if (clanId != 0)
            {
                return await getClan((int)clanId);
            }

            return NotFound();
        }

        [Authorize("database")]
        [HttpPost, Route("createClan")]
        public async Task<dynamic> createClan(int accountId, string clanName, int appId, string mediusStats)
        {
            // verify not already in clan
            var member = db.ClanMember.Where(c => c.IsActive == true && c.AccountId == accountId && c.Clan.AppId == appId)
                .FirstOrDefault();
            if (member != null)
                return BadRequest();

            // verify clan name doesn't already exist
            var existingClan = db.Clan.Where(c => c.IsActive == true && c.ClanName.ToLower() == clanName.ToLower() && c.AppId == appId)
                .FirstOrDefault();
            if (existingClan != null)
                return BadRequest();

            Clan newClan = new Clan()
            {
                ClanLeaderAccountId = accountId,
                ClanName = clanName,
                AppId = appId,
                CreatedBy = accountId,
                MediusStats = mediusStats
            };
            db.Clan.Add(newClan);
            db.SaveChanges();

            ClanMember newMember = new ClanMember()
            {
                ClanId = newClan.ClanId,
                AccountId = accountId,

            };
            db.ClanMember.Add(newMember);

            List<ClanStat> newStats = (from ds in db.DimClanStats
                                       select new ClanStat()
                                       {
                                           ClanId = newClan.ClanId,
                                           StatId = ds.StatId,
                                           StatValue = ds.DefaultValue
                                       }).ToList();
            db.ClanStat.AddRange(newStats);

            List<ClanCustomStat> newCustomStats = (from ds in db.DimClanCustomStats
                                       select new ClanCustomStat()
                                       {
                                           ClanId = newClan.ClanId,
                                           StatId = ds.StatId,
                                           StatValue = ds.DefaultValue
                                       }).ToList();
            db.ClanCustomStat.AddRange(newCustomStats);


            db.SaveChanges();
            return await getClan(newClan.ClanId);
        }

        [Authorize("database")]
        [HttpGet, Route("deleteClan")]
        public async Task<dynamic> deleteClan(int accountId, int clanId)
        {
            DateTime now = DateTime.UtcNow;
            Clan target = db.Clan.Where(c => c.ClanId == clanId && c.ClanLeaderAccountId == accountId)
                                    .Include(c => c.ClanMember)
                                    .Include(c => c.ClanInvitation)
                                    .FirstOrDefault();

            // not found
            if (target == null)
                return NotFound();

            target.IsActive = false;
            target.ModifiedBy = accountId;
            target.ModifiedDt = now;
            target.ClanMember.ToList().ForEach(cm =>
            {
                cm.IsActive = false;
                cm.ModifiedBy = accountId;
                cm.ModifiedDt = now;
            });
            target.ClanInvitation.ToList().ForEach(ci =>
            {
                ci.IsActive = false;
                ci.ModifiedBy = accountId;
                ci.ModifiedDt = now;
            });
            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpPost, Route("leaveClan")]
        public async Task<dynamic> leaveClan(int fromAccountId, int accountId, int clanId)
        {
            DateTime now = DateTime.UtcNow;
            ClanMember target = db.ClanMember.Where(cm => cm.AccountId == accountId && cm.ClanId == clanId && cm.IsActive == true).FirstOrDefault();

            target.IsActive = false;
            target.ModifiedDt = now;
            target.ModifiedBy = accountId;

            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpPost, Route("transferLeadership")]
        public async Task<dynamic> transferLeadership([FromBody] ClanTransferLeadershipDTO req) 
        {
            DateTime now = DateTime.UtcNow;
            var target = (from c in db.Clan where c.ClanId == req.ClanId && c.ClanLeaderAccountId == req.AccountId select c).FirstOrDefault();

            if (target == null)
                return NotFound();

            target.ClanLeaderAccountId = req.NewLeaderAccountId;
            target.ModifiedBy = req.AccountId;
            target.ModifiedDt = now;

            await db.SaveChangesAsync();
            return Ok();
        }

        [Authorize("database")]
        [HttpPost, Route("createInvitation")]
        public async Task<dynamic> createInvitation(int accountId, [FromBody] ClanInvitationDTO req)
        {
            Clan target = db.Clan.Where(c => c.ClanId == req.ClanId && c.ClanLeaderAccountId == accountId)
                                    .FirstOrDefault();

            var existingInvitation = db.ClanInvitation.Where(c => c.ClanId == req.ClanId && c.IsActive == true && c.AccountId == accountId)
                                    .FirstOrDefault();

            // prevent inviting someone twice
            if (existingInvitation != null)
            {
                return BadRequest();
            }

            if (target != null)
            {
                DateTime now = DateTime.UtcNow;
                ClanInvitation invite = new ClanInvitation()
                {
                    ClanId = req.ClanId,
                    AccountId = req.TargetAccountId,
                    InviteMsg = req.Message,
                    ResponseId = 0,
                    IsActive = true,
                };
                db.ClanInvitation.Add(invite);
                db.SaveChanges();

                return Ok();
            }

            return this.ValidationProblem();
        }

        [Authorize("database")]
        [HttpPost, Route("postClanMediusStats")]
        public async Task<dynamic> postClanMediusStats([FromBody] string StatsString, int ClanId)
        {
            Clan existingClan = db.Clan.Where(a => a.ClanId == ClanId).FirstOrDefault();
            if (existingClan == null)
                return NotFound();

            existingClan.MediusStats = StatsString;
            db.Clan.Attach(existingClan);
            db.Entry(existingClan).State = EntityState.Modified;
            db.SaveChanges();
            return Ok();
        }

        [Authorize("database")]
        [HttpGet, Route("invitations")]
        public async Task<dynamic> getInvitesByAccountId(int accountId)
        {
            ClanService cs = new ClanService();

            var invites = db.ClanInvitation.Where(ci => ci.AccountId == accountId && ci.ResponseId == 0 && ci.IsActive == true)
                                            .Include(ci => ci.Clan)
                                            .ThenInclude(c => c.ClanLeaderAccount)
                                            .Include(ci => ci.Account)
                                            .Select(ci => cs.toAccountClanInvitationDTO(ci))
                                            .ToList();

            return invites;

        }

        [Authorize("database")]
        [HttpPost, Route("respondInvitation")]
        public async Task<dynamic> respondInvitation([FromBody] ClanInvitationResponseDTO req)
        {
            DateTime now = DateTime.UtcNow;
            var target = (from ci in db.ClanInvitation where ci.Id == req.InvitationId && ci.AccountId == req.AccountId select ci).FirstOrDefault();

            if(target != null)
            {
                // client accepted invitation
                if (req.Response == 1 && target.IsActive == true)
                {
                    Clan clan = db.Clan.Where(c => c.ClanId == target.ClanId)
                                    .Include(c => c.ClanMember)
                                    .Include(c => c.ClanInvitation)
                                   .FirstOrDefault();
                    
                    if (clan != null)
                    {
                        clan.ClanMember.Add(new ClanMember()
                        {
                            ClanId = target.ClanId,
                            AccountId = target.AccountId,
                        });
                    }
                }

                target.ResponseDt = now;
                target.ResponseMsg = req.ResponseMessage;
                target.ResponseId = req.Response;
                target.IsActive = false;
                target.ModifiedBy = req.AccountId;
                target.ModifiedDt = now;

                db.SaveChanges();
                return Ok();
            }

            return NotFound();
        }

        [Authorize("database")]
        [HttpPost, Route("revokeInvitation")]
        public async Task<dynamic> revokeInvitation(int fromAccountId, int clanId, int targetAccountId)
        {
            DateTime now = DateTime.UtcNow;
            var target = (from ci in db.ClanInvitation where ci.AccountId == targetAccountId && ci.ClanId == clanId select ci).FirstOrDefault();

            if (target != null)
            {
                target.ResponseId = 3;
                target.ResponseDt = now;
                target.InviteMsg = "Invitation Revoked";
                target.IsActive = false;
                target.ModifiedBy = fromAccountId;
                target.ModifiedDt = now;

                db.SaveChanges();
                return Ok();
            }

            return NotFound();
        }

        [Authorize("database")]
        [HttpGet, Route("messages")]
        public async Task<dynamic> getClanMessages(int accountId, int clanId, int start, int pageSize)
        {
            ClanService cs = new ClanService();

            int totalMessages = db.ClanMessage.Where(cm => cm.ClanId == clanId && cm.IsActive == true).Count();

            int totalPages = (int) Math.Ceiling((decimal) totalMessages / pageSize);

            if (start < totalPages)
            {
                var skip = start * pageSize;

                var result = db.ClanMessage.Where(cm => cm.ClanId == clanId && cm.IsActive == true)
                                            .Skip(skip)
                                            .Take(pageSize)
                                            .Select(cm => cs.toClanMessageDTO(cm))
                                            .ToList();

                return result;
            }

            return NotFound($"Page index exceeds total of {totalPages}.");

        }

        [Authorize("database")]
        [HttpPost, Route("addMessage")]
        public async Task<dynamic> createClanMessage(int accountId, int clanId, [FromBody] ClanMessageDTO req)
        {
            ClanMessage newMessage = new ClanMessage()
            {
                ClanId = clanId,
                Message = req.Message,
                CreatedBy = accountId,
                IsActive = true,
            };

            db.ClanMessage.Add(newMessage);
            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpPut, Route("editMessage")]
        public async Task<dynamic> editClanMessage(int accountId, int clanId, [FromBody] ClanMessageDTO req)
        {
            var target = db.ClanMessage.Where(c => c.ClanId == clanId && c.Id == req.Id)
                .FirstOrDefault();

            if (target == null)
                return NotFound();

            target.Message = req.Message;

            db.ClanMessage.Attach(target);
            db.Entry(target).State = EntityState.Modified;
            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpPost, Route("deleteMessage")]
        public async Task<dynamic> deleteClanMessage(int accountId, int clanId, [FromBody] ClanMessageDTO req)
        {
            var target = db.ClanMessage.Where(c => c.ClanId == clanId && c.Id == req.Id)
                .FirstOrDefault();

            if (target == null)
                return NotFound();

            db.ClanMessage.Remove(target);
            db.Entry(target).State = EntityState.Deleted;
            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpPost, Route("requestClanTeamChallenge")]
        public async Task<dynamic> requestClanTeamChallenge(int challengerClanId, int againstClanId, int accountId, string message, int appId)
        {
            //ClanTeamChallenge clanTeamChallengeExists = db.ClanTeamChallenge.Where(a => a.ChallengerClanID == clanId).FirstOrDefault();

            //Clan challengerClan = db.Clan.Where(c => c.ClanId == challengerClanId && c.ClanLeaderAccountId == accountId)
            //                        .FirstOrDefault();

            //var clanTeamChallenge = db.ClanTeamChallenge.Where(c => c.ChallengerClanID == challengerClanId && c.AppId == appId).FirstOrDefault();

            ClanTeamChallenge newClanTeamChallenge = new ClanTeamChallenge()
            {
                AppId = appId,
                ChallengerClanID = challengerClanId,
                AgainstClanID = againstClanId,
                Status = 0,
                ResponseTime = 0,
                ChallengeMsg = message,
                ResponseMessage = null,
            };

            db.ClanTeamChallenge.Add(newClanTeamChallenge);
            db.Entry(newClanTeamChallenge).State = EntityState.Added;
            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpPost, Route("respondClanTeamChallenge")]
        public async Task<dynamic> respondClanTeamChallenge(int ClanChallengeId, int clanChallengeStatus, int accountId, string message, int appId)
        {
            //ClanTeamChallenge clanTeamChallengeExists = db.ClanTeamChallenge.Where(a => a.ChallengerClanID == clanId).FirstOrDefault();

            //Clan challengerClan = db.Clan.Where(c => c.ClanId == challengerClanId && c.ClanLeaderAccountId == accountId)
            //                        .FirstOrDefault();

            //var clanTeamChallenge = db.ClanTeamChallenge.Where(c => c.ChallengerClanID == challengerClanId && c.AppId == appId).FirstOrDefault();

            var target = db.ClanTeamChallenge.Where(c => c.ClanChallengeId == ClanChallengeId && c.AppId == appId)
                .FirstOrDefault();

            if (target == null)
                return NotFound();

            target.ResponseMessage = message;
            target.Status = clanChallengeStatus;

            db.ClanTeamChallenge.Attach(target);
            db.Entry(target).State = EntityState.Modified;
            db.SaveChanges();

            return Ok();
        }

        [Authorize("database")]
        [HttpGet, Route("getClanTeamChallenges")]
        public async Task<dynamic> getClanTeamChallenges(int clanId, int accountId, int clanChallengeStatus, int appId, int startIdx, int pageSize)
        {
            ClanService cs = new ClanService();

            //Clan clanTeamChallengeExists = db.Clan.Where(c => c.ClanId == clanId && c.ClanLeaderAccountId == accountId && c.AppId == appId).FirstOrDefault();

            //Console.WriteLine($"clanTeamChallenge exist?: {clanTeamChallengeExists.ClanName}");
            /*
            if (clanTeamChallengeExists == null)
                return NotFound();
            */

            int totalClanTeamChallenges = db.ClanTeamChallenge.Where(cm => (cm.AgainstClanID == clanId || cm.ChallengerClanID == clanId) && cm.Status == clanChallengeStatus && cm.AppId == appId).Count();

            int totalPages = (int)Math.Ceiling((decimal)totalClanTeamChallenges / pageSize);

            if (startIdx < totalPages)
            {
                var skip = startIdx * pageSize;

                var result = db.ClanTeamChallenge.Where(cm => (cm.AgainstClanID == clanId || cm.ChallengerClanID == clanId) && cm.Status == clanChallengeStatus && cm.AppId == appId)
                                            .Skip(skip)
                                            .Take(pageSize)
                                            .Select(cm => cs.toClanTeamChallengeDTO(cm));

                return result;
            } else
            {
                return BadRequest($"Page index exceeds total of {totalPages}.");
            }
        }

        [Authorize("database")]
        [HttpPost, Route("revokeClanTeamChallenge")]
        public async Task<dynamic> revokeClanTeamChallenge(int ClanChallengeId, int accountId, int appId)
        {
            //ClanTeamChallenge clanTeamChallengeExists = db.ClanTeamChallenge.Where(a => a.ChallengerClanID == clanId).FirstOrDefault();

            //Clan challengerClan = db.Clan.Where(c => c.ClanId == challengerClanId && c.ClanLeaderAccountId == accountId)
            //                        .FirstOrDefault();

            //var clanTeamChallenge = db.ClanTeamChallenge.Where(c => c.ChallengerClanID == challengerClanId && c.AppId == appId).FirstOrDefault();

            var target = db.ClanTeamChallenge.Where(c => c.ClanChallengeId == ClanChallengeId && c.AppId == appId)
                .FirstOrDefault();

            if (target == null)
                return NotFound();

            db.ClanTeamChallenge.Remove(target);
            db.Entry(target).State = EntityState.Deleted;
            db.SaveChanges();

            return Ok();
        }
    }
}
