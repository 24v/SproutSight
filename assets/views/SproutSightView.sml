<lane orientation="vertical" horizontal-content-alignment="middle" >
    <banner text="SproutSight Pro"  background={@Mods/StardewUI/Sprites/BannerBackground} background-border-thickness="48,0" padding="12" /> 
    <lane>
        <lane layout="150px content"
              margin="0, 16, 0, 0"
              orientation="vertical"
              horizontal-content-alignment="end"
              z-index="2">
            <frame *repeat={AllTabs}
                   layout="125px 64px"
                   margin={Margin}
                   padding="16, 0"
                   horizontal-content-alignment="middle"
                   vertical-content-alignment="middle"
                   background={@Mods/24v.SproutSight/Sprites/MenuTiles:TabButton}
                   focusable="true"
                   click=|^SelectTab(Value)|>
                <label text={Title} />
            </frame>
        </lane>

        <frame *switch={SelectedTab}
               layout="820px 500px"
               margin="0, 16, 0, 0"
               padding="32, 24"
               background={@Mods/StardewUI/Sprites/ControlBorder}>

            <scrollable peeking="128" layout="stretch">
                <lane *case="Today">
                    <scrollable peeking="128">
                        <lane>
                            <lane orientation="vertical">
                                <lane>
                                    <label text={TotalProceeds}/>
                                    <image layout="24px" margin="5,0,0,20" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                </lane>
                                <label *!if={ShippedSomething} margin="0,20,0,0" text="You have not shipped anything today." />
                                <lane *repeat={CurrentItems} tooltip={Name} vertical-content-alignment="middle">
                                    <panel *switch={QualityName} layout="64px" vertical-content-alignment="end">
                                        <image layout="stretch" margin="4" sprite={Sprite} />
                                        <lane vertical-content-alignment="end">
                                            <image *case="Silver" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarSilver} />
                                            <image *case="Gold" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarGold} />
                                            <image *case="Iridium" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarIridium} />
                                            <spacer layout="stretch 0px" />
                                            <digits layout="content" scale="3.0" number={StackCount} />
                                        </lane>
                                    </panel>
                                    <label text={TotalSalePrice} margin="10,0,5,0"/>
                                    <image layout="24px" margin="0,0,10,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                    <label text={FormattedSale}/>
                                </lane>
                            </lane>
                            <lane orientation="vertical" margin="120,0,0,0">
                                <lane>
                                    <label text="Other Sales"/>
                                    <image layout="24px" margin="5,0,0,10" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                </lane>
                                <lane layout="content 64px" horizontal-content-alignment="end">
                                    <label text="Received:" margin="10,20,0,0"/>
                                    <label text={TodayGoldIn} margin="10,20,0,0"/>
                                    <label text="g" margin="0,20,0,0"/>
                                </lane>
                                <lane layout="content 64px" horizontal-content-alignment="end">
                                    <label text="Spent:" margin="10,20,0,0"/>
                                    <label text={TodayGoldOut} color="Red" margin="10,20,0,0"/>
                                    <label text="g" margin="0,20,0,10"/>
                                </lane>
                            </lane>
                        </lane>
                    </scrollable>
                </lane>

                <!-- Shipping Tab -->
                <lane *case="Shipping" *switch={SelectedPeriod} *context={TrackedDataAggregator} 
                        layout="820px content" 
                        orientation="vertical" 
                        horizontal-content-alignment="end">

                    <!-- Header -->
                    <lane>
                        <label text={ShippedText} margin="0,0,10,10" />
                        <label text="(Note)" tooltip="Hover over year/season to see aggregated." scale=".5" layout="stretch"/>
                        <dropdown option-min-width="100" options={^Periods} selected-option={<>^SelectedPeriod} />
                        <dropdown option-min-width="100" options={^Operations} selected-option={<>^SelectedOperation} />
                    </lane>

                    <!-- Controls -->
                    <expander layout="stretch content"
                            margin="514,0,0,4"
                            header-padding="0,12"
                            header-background-tint="#99c"
                            is-expanded={<>^IsYearSelectionExpanded} >

                        <label layout="stretch" *outlet="header" text="Selected Years"/>
                        <lane orientation="vertical" margin="59,0,0,0" layout="stretch content" horizontal-content-alignment="start">
                            <checkbox *repeat={^YearSelectionOptions} label-text={Text} is-checked={IsChecked} click=|^^SelectYear(Year)|/>
                        </lane>
                    </expander>

                    <!-- All View -->
                    <lane *case="All" *repeat={ShippedYearsReversed} orientation="vertical">
                        <lane *repeat={SeasonElementsReversed} vertical-content-alignment="end"> 
                            <lane layout="140px 40px" vertical-content-alignment="end" tooltip={Tooltip}>
                                <image *if={IsSpring} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                                <image *if={IsSummer} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                                <image *if={IsFall} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                                <image *if={IsWinter} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                                <label text={Season} margin="5,0,0,0"/>
                            </lane>
                            <image *repeat={DayElements} tint={Tint} fit="Stretch" margin="1,40,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                            <lane *if={IsWinter} margin="0,0,18,0" tooltip={^Tooltip}>
                                <label text="Y-"/>
                                <label text={^Year} />
                                <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                        </lane>
                    </lane>

                    <!-- Season View -->
                    <lane *case="Season" vertical-content-alignment="end" layout="stretch content">
                        <label text="Seasons" margin="0,0,20,0" />
                        <lane *repeat={ShippedYears} margin="0,0,20,0" vertical-content-alignment="end">
                            <image *repeat={SeasonElements} tint={Tint} fit="Stretch" margin="1,40,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                        </lane>
                    </lane>

                    <!-- Year View -->
                    <lane *case="Year" vertical-content-alignment="end" layout="stretch content">
                        <label text="Years" margin="0,0,20,0" />
                        <image *repeat={ShippedYears} tint={Tint} fit="Stretch" margin="1,40,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                    </lane>
                </lane>  


                <!-- Wallet Tab -->
                <lane *case="Wallet" 
                        *context={TrackedDataAggregator} 
                        *switch={SelectedPeriod} 
                        orientation="vertical" >

                    <!-- Header -->
                    <lane>
                        <label text={WalletText} margin="0,0,10,10" />
                        <label text="(Note)" tooltip="Hover over year/season to see aggregated." scale=".5" layout="stretch"/>
                        <dropdown option-min-width="100" options={^Periods} selected-option={<>^SelectedPeriod} />
                        <dropdown option-min-width="100" options={^Operations} selected-option={<>^SelectedOperation} />
                    </lane>

                    <!-- Controls -->
                    <expander layout="stretch content"
                            margin="514,0,0,4"
                            header-padding="0,12"
                            header-background-tint="#99c"
                            is-expanded={<>^IsYearSelectionExpanded} >

                        <label layout="stretch" *outlet="header" text="Selected Years"/>
                        <lane orientation="vertical" margin="59,0,0,0" layout="stretch content" horizontal-content-alignment="start">
                            <checkbox *repeat={^YearSelectionOptions} label-text={Text} is-checked={IsChecked} click=|^^SelectYear(Year)|/>
                        </lane>
                    </expander>

                       <!-- All View -->
                    <lane *case="All" *repeat={WalletYearsReversed} orientation="vertical">
                        <lane *repeat={SeasonElementsReversed} vertical-content-alignment="end"> 
                            <lane layout="140px 40px" vertical-content-alignment="end" tooltip={Tooltip}>
                                <image *if={IsSpring} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                                <image *if={IsSummer} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                                <image *if={IsFall} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                                <image *if={IsWinter} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                                <label text={Season} margin="5,0,0,0"/>
                            </lane>
                            <image *repeat={DayElements} tint={Tint} fit="Stretch" margin="1,40,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                            <lane *if={IsWinter} margin="0,0,18,0" tooltip={^Tooltip}>
                                <label text="Y-"/>
                                <label text={^Year} />
                                <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                        </lane>
                    </lane>

                    <!-- Season View -->
                    <lane *case="Season" vertical-content-alignment="end" layout="stretch content">
                        <label text="Seasons" margin="0,0,20,0" />
                        <lane *repeat={WalletYears} margin="0,0,20,0" vertical-content-alignment="end">
                            <image *repeat={SeasonElements} tint={Tint} fit="Stretch" margin="1,40,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                        </lane>
                    </lane>

                    <!-- Year View -->
                    <lane *case="Year" vertical-content-alignment="end" layout="stretch content">
                        <label text="Years" margin="0,0,20,0" />
                        <image *repeat={WalletYears} tint={Tint} fit="Stretch" margin="1,40,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                    </lane>
                </lane>


                <lane *case="CashFlow" *context={TrackedDataAggregator}  *switch={SelectedPeriod} layout="820px content" orientation="vertical">
                    <lane>
                        <label text={CashFlowText} tooltip={CashFlowTooltip} margin="0,0,10,10" layout="stretch"/>
                        <dropdown option-min-width="100" options={^Periods} selected-option={<>^SelectedPeriod} />
                        <dropdown option-min-width="100" options={^Operations} selected-option={<>^SelectedOperation} />
                    </lane>
                    <lane *case="All" *repeat={CashFlowYearsReversed} orientation="vertical" margin="0,0,0,40">
                        <lane *repeat={SeasonElementsReversed} vertical-content-alignment="end" margin="0,0,0,10"> 
                            <lane layout="140px 40px" vertical-content-alignment="end" tooltip={Tooltip}>
                                <image *if={IsSpring} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                                <image *if={IsSummer} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                                <image *if={IsFall} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                                <image *if={IsWinter} layout="28px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                                <label text={Season} margin="5,0,0,0"/>
                            </lane>
                            <lane *repeat={DayElements} orientation="vertical" margin="1,0,0,0">
                                <image tint={Tint} fit="stretch" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                                <image tint={Tint2} fit="stretch" layout={Layout2} tooltip={Tooltip2} sprite={@Mods/StardewUI/Sprites/White} />
                            </lane>
                            <lane *if={IsWinter} margin="0,0,18,0" tooltip={^Tooltip}>
                                <label text="Y-"/>
                                <label text={^Year} />
                                <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                        </lane>
                    </lane>
                    <lane *case="Year" margin="0,0,0,40" vertical-content-alignment="end">
                        <lane *repeat={CashFlowYears} orientation="vertical" margin="1,0,0,0">
                            <image tint={Tint} fit="stretch" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                            <image tint={Tint2} fit="stretch" layout={Layout2} tooltip={Tooltip2} sprite={@Mods/StardewUI/Sprites/White} />
                        </lane>
                    </lane>
                </lane>
            </scrollable>
        </frame>
    </lane>
</lane>
